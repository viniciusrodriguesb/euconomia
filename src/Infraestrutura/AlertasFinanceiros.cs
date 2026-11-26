using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

public sealed class AlertasFinanceiros(FinanceiroDb banco, UnidadeFinanceira unidade)
{
    public async Task<string[]> AvaliarGravacaoAsync(Guid usuarioId, Lancamento[] novos, CancellationToken cancellationToken)
    {
        if (banco.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A avaliação na gravação exige a transação do comando.");
        await banco.SaveChangesAsync(cancellationToken);
        var meses = novos.Select(x => x.Competencia).Distinct().ToArray();
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var saldo = (await LeituraContabil.SaldosAsync(unidade, usuarioId, hoje, cancellationToken)).Sum(x => x.Saldo);
        var projecao = await ProjecaoCaixa.CalcularAsync(banco, unidade, usuarioId, hoje, 12, saldo, cancellationToken);
        var aportes = new List<OcorrenciaPrevista>();
        foreach (var mes in meses.Append(Mes(hoje)).Distinct().Where(x => x <= hoje))
            aportes.AddRange(await PrevisaoRecorrencias.ListarAsync(unidade, new ListarPrevisao(usuarioId, mes, 1), cancellationToken));
        var disparados = await AvaliarAsync(usuarioId, meses, cancellationToken);
        disparados.AddRange(await AvaliarProjecoesAsync(usuarioId, hoje, projecao, aportes, cancellationToken));
        return await RegistrarNovosAsync(usuarioId, disparados, cancellationToken);
    }

    private async Task<string[]> RegistrarNovosAsync(Guid usuarioId, List<Alerta> disparados, CancellationToken cancellationToken)
    {
        var chaves = disparados.Select(x => x.Referencia).Distinct().ToArray();
        var existentes = await unidade.Ler<Alerta>(usuarioId).Where(x => chaves.Contains(x.Referencia))
            .Select(x => new { x.Tipo, x.Competencia, x.Referencia }).ToListAsync(cancellationToken);
        var conhecidos = existentes.Select(x => (x.Tipo, x.Competencia, x.Referencia)).ToHashSet();
        var adicionados = disparados.Where(x => conhecidos.Add((x.Tipo, x.Competencia, x.Referencia))).ToArray();
        banco.AddRange(adicionados);
        return adicionados.Select(x => x.Mensagem).ToArray();
    }

    public async Task<List<Alerta>> AvaliarAsync(Guid usuarioId, DateOnly[] meses, CancellationToken cancellationToken)
    {
        var alertas = new List<Alerta>();
        if (meses.Length == 0)
            return alertas;

        var configuracao = await ConfiguracaoAsync(usuarioId, cancellationToken);
        var efetivos = Efetivos(usuarioId);
        var mesesHistoricos = meses.Min().AddMonths(-Math.Min(6, (meses.Min().Year - 1) * 12 + meses.Min().Month - 1));
        var mesFinal = meses.Max();
        var linhas = await efetivos.Where(x => x.Competencia >= mesesHistoricos && x.Competencia <= mesFinal).ToArrayAsync(cancellationToken);
        var estabelecimentos = await unidade.Ler<Estabelecimento>(usuarioId).ToDictionaryAsync(x => x.Id, cancellationToken);
        var cartoes = await unidade.Ler<Cartao>(usuarioId).Where(x => x.InativadoEm == null).ToDictionaryAsync(x => x.Id, cancellationToken);
        var faturas = unidade.Ler<Fatura>(usuarioId);
        var usoCartao = await efetivos.Where(x => x.FormaPagamento == FormaPagamento.CartaoCredito &&
                faturas.Any(f => f.Id == x.FaturaId && f.Status != StatusFatura.Paga))
            .GroupBy(x => x.CartaoId!.Value).Select(g => new { Cartao = g.Key, Total = g.Sum(x => x.Valor) }).ToDictionaryAsync(x => x.Cartao, x => x.Total, cancellationToken);

        foreach (var mes in meses)
        {
            var movimentosDoMes = linhas.Where(lancamento => lancamento.Competencia == mes).ToArray();
            var entradas = movimentosDoMes.Where(lancamento => lancamento.Tipo == TipoLancamento.Receita).Sum(lancamento => lancamento.Valor);
            var despesas = movimentosDoMes.Where(lancamento => lancamento.Tipo == TipoLancamento.Despesa).ToArray();

            alertas.AddRange(AvaliarLimitesMensais(usuarioId, mes, entradas, despesas, configuracao));
            alertas.AddRange(AvaliarEstabelecimentos(usuarioId, mes, despesas, estabelecimentos, configuracao));
            alertas.AddRange(AvaliarDespesasAtipicas(usuarioId, mes, despesas, linhas, configuracao));
            alertas.AddRange(AvaliarLimitesDosCartoes(usuarioId, mes, cartoes.Values, usoCartao, configuracao));
        }

        return alertas.Where(x => Habilitado(configuracao, x.Tipo)).ToList();
    }

    private static IEnumerable<Alerta> AvaliarLimitesMensais(Guid usuarioId, DateOnly mes, decimal entradas,
        Lancamento[] despesas, ConfiguracaoUsuario configuracao)
    {
        var gasto = despesas.Sum(x => x.Valor);
        var fixos = despesas.Where(x => x.RegraRecorrenteId.HasValue).Sum(x => x.Valor);
        if (entradas > 0)
        {
            var percentual = 100 * gasto / entradas;
            if (percentual > configuracao.PercentualGastoAmarelo)
            {
                var vermelho = percentual > configuracao.PercentualGastoVermelho;
                yield return Criar(usuarioId, TipoAlerta.GastoAlto, mes, vermelho ? "vermelho" : "amarelo", "As despesas do mês passaram do seu limite de alerta.",
                    vermelho ? GravidadeAlerta.Vermelho : GravidadeAlerta.Amarelo);
            }
            if (100 * fixos / entradas > configuracao.PercentualCustosFixos)
                yield return Criar(usuarioId, TipoAlerta.CustosFixos, mes, "fixos", "Os custos fixos passaram do limite em relação às entradas.");
        }
    }

    private static IEnumerable<Alerta> AvaliarEstabelecimentos(Guid usuarioId, DateOnly mes, Lancamento[] despesas,
        Dictionary<Guid, Estabelecimento> estabelecimentos, ConfiguracaoUsuario configuracao)
    {
        foreach (var grupo in despesas.Where(x => x.EstabelecimentoId.HasValue)
            .GroupBy(x => estabelecimentos[x.EstabelecimentoId!.Value].MescladoEmId ?? x.EstabelecimentoId.Value))
        {
            var loja = estabelecimentos[grupo.Key];
            var limite = loja.LimiteTransacoes ?? configuracao.LimiteTransacoesEstabelecimento;
            if (grupo.Select(x => x.CompraParceladaId ?? x.Id).Distinct().Count() >= limite)
                yield return Criar(usuarioId, TipoAlerta.MesmoEstabelecimento, mes, loja.Id.ToString(), $"Você atingiu {limite} transações no mês em {loja.Nome[..Math.Min(110, loja.Nome.Length)]}.");
        }
    }

    private static IEnumerable<Alerta> AvaliarDespesasAtipicas(Guid usuarioId, DateOnly mes, Lancamento[] despesas,
        Lancamento[] linhas, ConfiguracaoUsuario configuracao)
    {
        foreach (var categoria in despesas.Where(x => x.CategoriaId.HasValue).GroupBy(x => x.CategoriaId))
        {
            var inicio = mes.AddMonths(-Math.Min(6, (mes.Year - 1) * 12 + mes.Month - 1));
            var anteriores = linhas.Where(x => x.Tipo == TipoLancamento.Despesa && x.CategoriaId == categoria.Key &&
                x.Competencia >= inicio && x.Competencia < mes).GroupBy(x => x.CompraParceladaId ?? x.Id).ToArray();
            if (anteriores.Length == 0)
                continue;
            var limite = configuracao.MultiplicadorDespesaAtipica * anteriores.Average(x => x.Sum(y => y.Valor));
            foreach (var despesa in categoria.GroupBy(x => x.CompraParceladaId ?? x.Id).Where(x => x.Sum(y => y.Valor) > limite))
                yield return Criar(usuarioId, TipoAlerta.DespesaAtipica, mes, despesa.Key.ToString(), "Esta despesa supera o limite baseado na média da categoria.");
        }
    }

    private static IEnumerable<Alerta> AvaliarLimitesDosCartoes(Guid usuarioId, DateOnly mes, IEnumerable<Cartao> cartoes,
        Dictionary<Guid, decimal> usoCartao, ConfiguracaoUsuario configuracao)
    {
        foreach (var cartao in cartoes)
        {
            if (100 * usoCartao.GetValueOrDefault(cartao.Id) / cartao.Limite > configuracao.PercentualLimiteCartao)
                yield return Criar(usuarioId, TipoAlerta.LimiteCartao, mes, cartao.Id.ToString(), $"O uso do limite do cartão {cartao.Nome[..Math.Min(110, cartao.Nome.Length)]} passou do alerta.");
        }
    }

    public async Task<List<Alerta>> AvaliarProjecoesAsync(Guid usuarioId, DateOnly hoje, ProjecaoMes[] projecao,
        IEnumerable<OcorrenciaPrevista> previstos, CancellationToken cancellationToken)
    {
        var configuracao = await ConfiguracaoAsync(usuarioId, cancellationToken);
        var alertas = new List<Alerta>();
        var atual = Mes(hoje);
        var inicioMedia = atual.AddMonths(-6);
        var efetivos = Efetivos(usuarioId);
        var media = await efetivos.Where(x => x.Tipo == TipoLancamento.Receita && x.Competencia >= inicioMedia && x.Competencia < atual)
            .SumAsync(x => x.Valor, cancellationToken) / 6m;
        foreach (var p in projecao)
        {
            if (p.SaldoAoFim < 0)
                alertas.Add(Criar(usuarioId, TipoAlerta.SaldoProjetado, p.Mes, "projecao", $"Saldo projetado negativo em {p.Mes:MM/yyyy}.", GravidadeAlerta.Vermelho));
            if (media > 0 && 100 * (p.Fixos + p.ParcelasEFaturas) / media > configuracao.PercentualComprometimento)
                alertas.Add(Criar(usuarioId, TipoAlerta.ComprometimentoFuturo, p.Mes, "projecao", $"Comprometimento acima do limite em {p.Mes:MM/yyyy}."));
        }

        var metas = previstos.Where(x => x.Tipo == TipoRecorrencia.AporteFixo && !x.Ignorada && x.Data <= hoje)
            .GroupBy(x => x.Competencia).ToDictionary(x => x.Key, x => x.Sum(y => y.Valor));
        var meses = metas.Keys.ToArray();
        var realizados = await efetivos.Where(x => x.Tipo == TipoLancamento.AporteInvestimento && meses.Contains(x.Competencia))
            .GroupBy(x => x.Competencia).Select(g => new { Mes = g.Key, Valor = g.Sum(x => x.Valor) })
            .ToDictionaryAsync(x => x.Mes, x => x.Valor, cancellationToken);
        foreach (var (mes, valor) in metas)
        {
            if (realizados.GetValueOrDefault(mes) < valor)
                alertas.Add(Criar(usuarioId, TipoAlerta.AporteInsuficiente, mes, "aporte", "O aporte realizado está abaixo do planejado."));
        }
        return alertas.Where(x => Habilitado(configuracao, x.Tipo)).ToList();
    }

    private IQueryable<Lancamento> Efetivos(Guid usuarioId)
    {
        var ledger = unidade.Ler<Lancamento>(usuarioId);
        return ledger.Where(x => x.Tipo != TipoLancamento.Estorno && !ledger.Any(e => e.LancamentoEstornadoId == x.Id));
    }

    private async Task<ConfiguracaoUsuario> ConfiguracaoAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        await banco.Set<ConfiguracaoUsuario>().AsNoTracking().SingleOrDefaultAsync(x => x.UsuarioId == usuarioId, cancellationToken)
            ?? new ConfiguracaoUsuario { UsuarioId = usuarioId };

    private static Alerta Criar(Guid usuarioId, TipoAlerta tipo, DateOnly mes, string referencia, string mensagem,
        GravidadeAlerta gravidade = GravidadeAlerta.Amarelo) =>
        new() { UsuarioId = usuarioId, Tipo = tipo, Competencia = mes, Referencia = referencia, Mensagem = mensagem, Gravidade = gravidade };

    private static bool Habilitado(ConfiguracaoUsuario configuracao, TipoAlerta tipo) =>
        (configuracao.AlertasDesativados & (1 << ((int)tipo - 1))) == 0;
}
