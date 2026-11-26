using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

public sealed class RegistroLancamentos(FinanceiroDb banco, UnidadeFinanceira unidade)
{
    public async Task<EfeitoLancamento> PreverAsync(Guid usuarioId, DadosLancamento dados, CancellationToken cancellationToken)
    {
        var cartao = await LancamentoValidador.ValidarAsync(unidade, usuarioId, dados, cancellationToken);
        if (cartao is null)
            return new(ImpactoCaixa(dados.Tipo, dados.FormaPagamento, dados.Valor),
                dados.Competencia ?? Mes(dados.DataLancamento), []);

        var previstas = await PreverFaturasAsync(usuarioId, dados, cartao, cancellationToken);
        return new(0, dados.Competencia ?? Mes(dados.DataLancamento), previstas);
    }

    public async Task<Lancamento[]> CriarAsync(Guid usuarioId, DadosLancamento dados, Guid? regraId, CancellationToken cancellationToken)
    {
        var cartao = await LancamentoValidador.ValidarAsync(unidade, usuarioId, dados, cancellationToken);
        var competencia = dados.Competencia ?? Mes(dados.DataLancamento);
        if (cartao is null)
        {
            LancamentoValidador.ConferirEfeito(dados.EfeitoEsperado, competencia, ImpactoCaixa(dados.Tipo, dados.FormaPagamento, dados.Valor), []);
            var lancamento = MontarLancamento(usuarioId, dados, dados.Valor, competencia, dados.ChaveIdempotencia, regraId);
            banco.Add(lancamento);
            return [lancamento];
        }

        var previstas = await PreverFaturasAsync(usuarioId, dados, cartao, cancellationToken);
        LancamentoValidador.ConferirEfeito(dados.EfeitoEsperado, competencia, 0, previstas);

        return await RegistrarParcelasAsync(usuarioId, dados, regraId, cartao.Id, competencia, previstas, cancellationToken);
    }

    private async Task<Lancamento[]> RegistrarParcelasAsync(Guid usuarioId, DadosLancamento dados, Guid? regraId,
        Guid cartaoId, DateOnly competencia, ParcelaPrevista[] previstas, CancellationToken cancellationToken)
    {
        var meses = previstas.Select(parcela => parcela.Competencia).ToArray();
        var faturas = await ListarFaturasAsync(usuarioId, cartaoId, meses, cancellationToken);
        var compra = dados.Parcelas > 1 ? CriarCompraParcelada(usuarioId, cartaoId, dados) : null;
        var criados = new List<Lancamento>(previstas.Length);

        foreach (var parcela in previstas)
        {
            if (!faturas.TryGetValue(parcela.Competencia, out var fatura))
                fatura = CriarFatura(usuarioId, cartaoId, parcela);

            LancamentoValidador.ValidarFaturaAbertaParaCompra(fatura);

            var lancamento = MontarLancamento(usuarioId, dados, parcela.Valor, competencia,
                parcela.Numero == 1 ? dados.ChaveIdempotencia : Guid.CreateVersion7(), regraId);
            lancamento.CartaoId = cartaoId;
            lancamento.FaturaId = fatura.Id;
            if (compra is not null)
            {
                lancamento.CompraParceladaId = compra.Id;
                lancamento.NumeroParcela = parcela.Numero;
                lancamento.TotalParcelas = compra.TotalParcelas;
            }

            criados.Add(lancamento);
            banco.Add(lancamento);
        }

        return criados.ToArray();
    }

    private CompraParcelada CriarCompraParcelada(Guid usuarioId, Guid cartaoId, DadosLancamento dados)
    {
        var compra = new CompraParcelada
        {
            UsuarioId = usuarioId,
            CartaoId = cartaoId,
            DataCompra = dados.DataLancamento,
            TotalParcelas = dados.Parcelas,
            ValorTotal = dados.Valor
        };
        banco.Add(compra);

        return compra;
    }

    private Fatura CriarFatura(Guid usuarioId, Guid cartaoId, ParcelaPrevista parcela)
    {
        var fatura = new Fatura
        {
            UsuarioId = usuarioId,
            CartaoId = cartaoId,
            Competencia = parcela.Competencia,
            DataFechamento = parcela.Fechamento,
            DataVencimento = parcela.Vencimento
        };
        banco.Add(fatura);

        return fatura;
    }

    private async Task<ParcelaPrevista[]> PreverFaturasAsync(Guid usuarioId, DadosLancamento dados, Cartao cartao, CancellationToken cancellationToken)
    {
        var previstas = PreverFaturas(dados.DataLancamento, cartao, dados.Valor, dados.Parcelas);
        var primeira = previstas[0];
        var inicio = Mes(dados.DataLancamento);
        var existente = await unidade.Ler<Fatura>(usuarioId).SingleOrDefaultAsync(x => x.CartaoId == cartao.Id && x.Competencia == inicio, cancellationToken);
        if (existente is not null && (dados.DataLancamento <= existente.DataFechamento) != (primeira.Competencia == inicio))
        {
            var parcelas = Parcelar(dados.Valor, dados.Parcelas);
            var mesInicial = dados.DataLancamento <= existente.DataFechamento ? inicio : inicio.AddMonths(1);
            previstas = parcelas.Select((valor, indice) =>
            {
                var mes = mesInicial.AddMonths(indice);
                return PreverFaturas(mes, cartao, valor, 1)[0] with { Numero = indice + 1 };
            }).ToArray();
        }

        var meses = previstas.Select(parcela => parcela.Competencia).ToArray();
        var existentes = await ListarFaturasAsync(usuarioId, cartao.Id, meses, cancellationToken);

        return previstas.Select(parcela =>
        {
            if (!existentes.TryGetValue(parcela.Competencia, out var fatura))
                return parcela;

            LancamentoValidador.ValidarFaturaAbertaParaCompra(fatura);

            return parcela with { Fechamento = fatura.DataFechamento, Vencimento = fatura.DataVencimento };
        }).ToArray();
    }

    private Task<Dictionary<DateOnly, Fatura>> ListarFaturasAsync(Guid usuarioId, Guid cartaoId, DateOnly[] meses,
        CancellationToken cancellationToken) =>
        unidade.Ler<Fatura>(usuarioId)
            .Where(fatura => fatura.CartaoId == cartaoId && meses.Contains(fatura.Competencia))
            .ToDictionaryAsync(fatura => fatura.Competencia, cancellationToken);

    private static Lancamento MontarLancamento(Guid usuarioId, DadosLancamento dados, decimal valor, DateOnly competencia,
        Guid chave, Guid? regraId) => new()
        {
            UsuarioId = usuarioId,
            Tipo = dados.Tipo,
            FormaPagamento = dados.FormaPagamento,
            Valor = valor,
            DataLancamento = dados.DataLancamento,
            Competencia = competencia,
            ContaId = dados.ContaId,
            CategoriaId = dados.CategoriaId,
            EstabelecimentoId = dados.EstabelecimentoId,
            Descricao = dados.Descricao.Trim(),
            ChaveIdempotencia = chave,
            RegraRecorrenteId = regraId
        };
}
