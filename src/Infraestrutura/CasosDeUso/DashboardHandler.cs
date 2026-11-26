using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using MediatR;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

public sealed class DashboardHandler(FinanceiroDb banco, UnidadeFinanceira unidade, AlertasFinanceiros alertas) : IRequestHandler<ObterDashboard, Painel>
{
    public async Task<Painel> Handle(ObterDashboard requisicao, CancellationToken cancellationToken)
    {
        DashboardValidador.Validar(requisicao);

        var usuarioId = requisicao.UsuarioId;
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var mesAtual = Mes(hoje);
        var fim = requisicao.Inicio.AddMonths(requisicao.Meses - 1);
        var desde = requisicao.Inicio.AddMonths(-6);
        var limitePeriodo = fim.AddMonths(1);
        var porCaixa = requisicao.Visao == VisaoRelatorio.Caixa;
        var movimentos = LeituraContabil.Movimentos(unidade, usuarioId);

        await using var leitura = await banco.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        var linhas = await (porCaixa
            ? movimentos.Where(movimento => movimento.Data >= desde && movimento.Data < limitePeriodo && !movimento.Credito)
            : movimentos.Where(movimento => movimento.Competencia >= desde && movimento.Competencia <= fim))
            .ToArrayAsync(cancellationToken);

        var movimentosPorMes = linhas.ToLookup(movimento => CompetenciaDaVisao(movimento, porCaixa));
        var meses = Enumerable.Range(0, requisicao.Meses).Select(requisicao.Inicio.AddMonths).ToArray();
        var balanco = meses.Select(mes => ResumirMes(mes, movimentosPorMes[mes])).ToArray();
        var ultimosSeisMeses = Enumerable.Range(1, 6).Select(indice => requisicao.Inicio.AddMonths(-indice))
            .Select(mes => ResumirMes(mes, movimentosPorMes[mes])).ToArray();

        var saldos = await LeituraContabil.SaldosAsync(unidade, usuarioId, hoje, cancellationToken);
        var saldo = saldos.Sum(conta => conta.Saldo);
        var (patrimonio, seriePatrimonio) = await CalcularPatrimonioAsync(movimentos, hoje, meses, limitePeriodo, cancellationToken);
        var previsoesPeriodo = await PrevisaoRecorrencias.ListarAsync(unidade, new ListarPrevisao(usuarioId, requisicao.Inicio, requisicao.Meses), cancellationToken);
        var projecao = await ProjecaoCaixa.CalcularAsync(banco, unidade, usuarioId, hoje, requisicao.Horizonte, saldo, cancellationToken);
        var faturas = await LeituraFaturas.ListarAsync(banco, unidade, new ListarFaturas(usuarioId, mesAtual.AddMonths(-1), requisicao.Horizonte + 1), cancellationToken);
        var valoresRealizados = await ObterValoresRealizadosAsync(movimentos, requisicao.Inicio, fim, cancellationToken);
        var evolucao = await CalcularEvolucaoAsync(usuarioId, movimentos, movimentosPorMes, desde, limitePeriodo, requisicao.Meses, porCaixa, cancellationToken);
        var alertasExibidos = await ObterAlertasAsync(usuarioId, hoje, meses, projecao, previsoesPeriodo, cancellationToken);
        var despesas = linhas.Where(movimento => CompetenciaDaVisao(movimento, porCaixa) >= requisicao.Inicio &&
            movimento.Tipo == TipoLancamento.Despesa).ToArray();
        var (rankingCategorias, rankingEstabelecimentos) = await ObterRankingsAsync(usuarioId, despesas, cancellationToken);

        await leitura.CommitAsync(cancellationToken);

        return new(balanco, projecao, saldo, patrimonio, projecao[0].SaldoAoFim, ultimosSeisMeses[0],
            ultimosSeisMeses.Average(mes => mes.Entradas), ultimosSeisMeses.Average(mes => CalcularSaidas(mes, porCaixa)),
            rankingCategorias, rankingEstabelecimentos, faturas, previsoesPeriodo, alertasExibidos,
            balanco.Any(mes => mes.Entradas <= 0) ? "Cadastre suas entradas do mês para ativar este alerta." : null,
            seriePatrimonio, valoresRealizados, evolucao);
    }

    private static DateOnly CompetenciaDaVisao(Movimento movimento, bool porCaixa) =>
        porCaixa ? Mes(movimento.Data) : movimento.Competencia;

    private static decimal CalcularSaidas(ResumoMes resumo, bool porCaixa) =>
        resumo.Despesas + resumo.Aportes + resumo.Retiradas + (porCaixa ? resumo.Pagamentos : 0);

    private static ResumoMes ResumirMes(DateOnly mes, IEnumerable<Movimento> itens)
    {
        decimal Valor(TipoLancamento tipo) => itens.Where(x => x.Tipo == tipo).Sum(x => x.Valor);
        return new(mes, Valor(TipoLancamento.Receita), Valor(TipoLancamento.Despesa),
            itens.Where(x => x.Tipo == TipoLancamento.Despesa && x.Fixo).Sum(x => x.Valor),
            itens.Where(x => x.Tipo == TipoLancamento.Despesa && x.Credito).Sum(x => x.Valor),
            Valor(TipoLancamento.AporteInvestimento), Valor(TipoLancamento.ResgateInvestimento), Valor(TipoLancamento.RetiradaSaldo),
            Valor(TipoLancamento.PagamentoFatura), itens.Sum(x => ImpactoCaixa(x.Tipo, x.Credito ? FormaPagamento.CartaoCredito : FormaPagamento.Pix, x.Valor)),
            itens.Where(x => x.Tipo == TipoLancamento.Despesa && x.Fixo && x.Credito).Sum(x => x.Valor));
    }

    private static async Task<(decimal Atual, PatrimonioMes[] Serie)> CalcularPatrimonioAsync(
        IQueryable<Movimento> movimentos, DateOnly hoje, DateOnly[] meses, DateOnly limitePeriodo, CancellationToken cancellationToken)
    {
        var investimentos = movimentos.Where(x => x.Tipo == TipoLancamento.AporteInvestimento || x.Tipo == TipoLancamento.ResgateInvestimento);
        var patrimonio = await investimentos.Where(x => x.Data <= hoje).SumAsync(x => x.Tipo == TipoLancamento.AporteInvestimento ? x.Valor : -x.Valor, cancellationToken);
        var patrimonioInicial = await investimentos.Where(x => x.Data < meses[0]).SumAsync(x => x.Tipo == TipoLancamento.AporteInvestimento ? x.Valor : -x.Valor, cancellationToken);
        var patrimonioMensal = await investimentos.Where(x => x.Data >= meses[0] && x.Data < limitePeriodo)
            .GroupBy(x => new { x.Data.Year, x.Data.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Valor = g.Sum(x => x.Tipo == TipoLancamento.AporteInvestimento ? x.Valor : -x.Valor) })
            .ToDictionaryAsync(x => new DateOnly(x.Year, x.Month, 1), x => x.Valor, cancellationToken);

        var seriePatrimonio = meses.Select(mes =>
        {
            patrimonioInicial += patrimonioMensal.GetValueOrDefault(mes);
            return new PatrimonioMes(mes, patrimonioInicial);
        }).ToArray();

        return (patrimonio, seriePatrimonio);
    }

    private static Task<RealizadoRegra[]> ObterValoresRealizadosAsync(IQueryable<Movimento> movimentos,
        DateOnly inicio, DateOnly fim, CancellationToken cancellationToken) =>
        movimentos.Where(movimento => movimento.Competencia >= inicio && movimento.Competencia <= fim && movimento.RegraId.HasValue)
            .GroupBy(movimento => new { movimento.RegraId, movimento.Competencia })
            .Select(grupo => new RealizadoRegra(grupo.Key.RegraId!.Value, grupo.Key.Competencia, grupo.Sum(movimento => movimento.Valor)))
            .ToArrayAsync(cancellationToken);

    private async Task<EvolucaoMes[]> CalcularEvolucaoAsync(Guid usuarioId, IQueryable<Movimento> movimentos,
        ILookup<DateOnly, Movimento> movimentosPorMes, DateOnly desde, DateOnly limitePeriodo, int meses, bool porCaixa,
        CancellationToken cancellationToken)
    {
        var saldoNoInicioDaSerie = (await LeituraContabil.SaldosAsync(unidade, usuarioId, desde.AddDays(-1), cancellationToken)).Sum(x => x.Saldo);
        var efetivoMensal = await movimentos.Where(x => !x.Credito && x.Data >= desde && x.Data < limitePeriodo)
            .GroupBy(x => new { x.Data.Year, x.Data.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Valor = g.Sum(x => x.Tipo == TipoLancamento.Receita || x.Tipo == TipoLancamento.ResgateInvestimento ? x.Valor : -x.Valor) })
            .ToDictionaryAsync(x => new DateOnly(x.Year, x.Month, 1), x => x.Valor, cancellationToken);
        var saldosIniciais = await unidade.Ler<Conta>(usuarioId).Where(x => x.DataSaldoInicial >= desde && x.DataSaldoInicial < limitePeriodo)
            .GroupBy(x => new { x.DataSaldoInicial.Year, x.DataSaldoInicial.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Valor = g.Sum(x => x.SaldoInicial) })
            .ToDictionaryAsync(x => new DateOnly(x.Year, x.Month, 1), x => x.Valor, cancellationToken);
        return Enumerable.Range(0, meses + 6).Select(desde.AddMonths).Select(mes =>
        {
            saldoNoInicioDaSerie += efetivoMensal.GetValueOrDefault(mes) + saldosIniciais.GetValueOrDefault(mes);
            var resumo = ResumirMes(mes, movimentosPorMes[mes]);
            return new EvolucaoMes(mes, resumo.Entradas, CalcularSaidas(resumo, porCaixa), saldoNoInicioDaSerie);
        }).ToArray();
    }

    private async Task<AlertaExibido[]> ObterAlertasAsync(Guid usuarioId, DateOnly hoje, DateOnly[] meses,
        ProjecaoMes[] projecao, OcorrenciaPrevista[] previsoesPeriodo, CancellationToken cancellationToken)
    {
        var ativos = await alertas.AvaliarAsync(usuarioId, meses, cancellationToken);
        ativos.AddRange(await alertas.AvaliarProjecoesAsync(usuarioId, hoje, projecao, previsoesPeriodo, cancellationToken));
        var mesesComAlertas = ativos.Select(x => x.Competencia).Distinct().ToArray();
        var referenciasAlertas = ativos.Select(x => x.Referencia).Distinct().ToArray();
        var armazenados = await unidade.Ler<Alerta>(usuarioId).Where(x => mesesComAlertas.Contains(x.Competencia) && referenciasAlertas.Contains(x.Referencia))
            .ToDictionaryAsync(x => (x.Tipo, x.Competencia, x.Referencia), cancellationToken);

        return ativos.Select(alerta =>
        {
            var salvo = armazenados.GetValueOrDefault((alerta.Tipo, alerta.Competencia, alerta.Referencia));
            return new AlertaExibido(salvo?.Id, alerta.Tipo, alerta.Gravidade, alerta.Competencia,
                alerta.Mensagem, alerta.Referencia, salvo?.LidoEm);
        }).ToArray();
    }

    private async Task<(Ranking[] Categorias, Ranking[] Estabelecimentos)> ObterRankingsAsync(
        Guid usuarioId, Movimento[] despesas, CancellationToken cancellationToken)
    {
        var categorias = await unidade.Ler<Categoria>(usuarioId).ToDictionaryAsync(x => x.Id, x => x.Nome, cancellationToken);
        var lojas = await unidade.Ler<Estabelecimento>(usuarioId).ToDictionaryAsync(x => x.Id, cancellationToken);
        var nomesLojas = lojas.Values.Where(x => !x.MescladoEmId.HasValue).ToDictionary(x => x.Id, x => x.Nome);

        return (
            MontarRanking(despesas, movimento => movimento.CategoriaId, categorias),
            MontarRanking(despesas, movimento => movimento.EstabelecimentoId is { } id ? lojas[id].MescladoEmId ?? id : null, nomesLojas));
    }

    private static Ranking[] MontarRanking(Movimento[] despesas, Func<Movimento, Guid?> selecionarId, Dictionary<Guid, string> nomes) =>
        despesas.Where(movimento => selecionarId(movimento).HasValue)
            .GroupBy(movimento => selecionarId(movimento)!.Value)
            .Select(grupo => new Ranking(nomes.GetValueOrDefault(grupo.Key, "Cadastro"), grupo.Sum(movimento => movimento.Valor),
                grupo.Where(movimento => movimento.NumeroParcela is null or 1).Sum(movimento => movimento.Valor > 0 ? 1 : -1)))
            .OrderByDescending(ranking => ranking.Valor).ToArray();
}
