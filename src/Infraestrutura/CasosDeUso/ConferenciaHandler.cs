using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using MediatR;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

public sealed class ConferenciaHandler(FinanceiroDb banco, UnidadeFinanceira unidade) : IRequestHandler<ConferirFinanceiro, ResultadoConferencia>
{
    public async Task<ResultadoConferencia> Handle(ConferirFinanceiro requisicao, CancellationToken cancellationToken)
    {
        var usuario = requisicao.UsuarioId;
        await using var transacao = await banco.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        var divergencias = new List<string>();
        var lancamentos = unidade.Ler<Lancamento>(usuario);
        var faturas = await unidade.Ler<Fatura>(usuario).ToArrayAsync(cancellationToken);

        await ConferirFaturasAsync(usuario, lancamentos, faturas, divergencias, cancellationToken);
        var quantidadeCompras = await ConferirComprasAsync(usuario, lancamentos, faturas, divergencias, cancellationToken);
        var quantidadeContas = await ConferirSaldosAsync(usuario, lancamentos, divergencias, cancellationToken);

        await transacao.CommitAsync(cancellationToken);
        return new(quantidadeContas, faturas.Length, quantidadeCompras, divergencias.ToArray());
    }

    private async Task ConferirFaturasAsync(Guid usuario, IQueryable<Lancamento> lancamentos, Fatura[] faturas,
        List<string> divergencias, CancellationToken cancellationToken)
    {
        var totais = await LeituraFaturas.Totais(banco, usuario).ToDictionaryAsync(x => x.FaturaId, x => x.Total, cancellationToken);
        var recomputados = await lancamentos.Where(x => x.FormaPagamento == FormaPagamento.CartaoCredito)
            .GroupBy(x => x.FaturaId!.Value).Select(g => new { Id = g.Key, Total = g.Sum(x => x.Tipo == TipoLancamento.Estorno ? -x.Valor : x.Valor) })
            .ToDictionaryAsync(x => x.Id, x => x.Total, cancellationToken);
        foreach (var f in faturas)
        {
            var total = recomputados.GetValueOrDefault(f.Id);
            if (!totais.TryGetValue(f.Id, out var exibido) || total != exibido)
                divergencias.Add($"Fatura {f.Id}: o total exibido diverge do lancamentos.");
            if (f.LancamentoPagamentoId is { } pagamento && !await lancamentos.AnyAsync(x => x.Id == pagamento &&
                x.Tipo == TipoLancamento.PagamentoFatura && x.FaturaId == f.Id && x.Valor == total, cancellationToken))
                divergencias.Add($"Fatura {f.Id}: o pagamento ativo diverge do total.");
        }
    }

    private async Task<int> ConferirComprasAsync(Guid usuario, IQueryable<Lancamento> lancamentos, Fatura[] faturas,
        List<string> divergencias, CancellationToken cancellationToken)
    {
        var compras = await unidade.Ler<CompraParcelada>(usuario).ToArrayAsync(cancellationToken);
        var parcelas = await lancamentos.Where(x => x.CompraParceladaId.HasValue)
            .Select(x => new { Id = x.CompraParceladaId!.Value, Numero = x.NumeroParcela!.Value, x.Valor, x.TotalParcelas, x.FaturaId, x.CartaoId })
            .ToArrayAsync(cancellationToken);
        var porCompra = parcelas.ToLookup(x => x.Id);
        var porFatura = faturas.ToDictionary(x => x.Id);
        foreach (var compra in compras)
        {
            var itens = porCompra[compra.Id].OrderBy(x => x.Numero).ToArray();
            var esperado = Parcelar(compra.ValorTotal, compra.TotalParcelas);
            if (itens.Length != esperado.Length || !itens.Select(x => x.Valor).SequenceEqual(esperado) ||
                !itens.Select(x => x.Numero).SequenceEqual(Enumerable.Range(1, esperado.Length)) ||
                itens.Any(x => x.CartaoId != compra.CartaoId || x.TotalParcelas != compra.TotalParcelas) ||
                itens.Zip(itens.Skip(1)).Any(x => porFatura[x.First.FaturaId!.Value].Competencia.AddMonths(1) != porFatura[x.Second.FaturaId!.Value].Competencia))
                divergencias.Add($"Compra {compra.Id}: as parcelas divergem da compra.");
        }

        return compras.Length;
    }

    private async Task<int> ConferirSaldosAsync(Guid usuario, IQueryable<Lancamento> lancamentos, List<string> divergencias,
        CancellationToken cancellationToken)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var contas = await unidade.Ler<Conta>(usuario).ToArrayAsync(cancellationToken);
        var saldos = await LeituraContabil.SaldosAsync(unidade, usuario, hoje, cancellationToken);
        var linhas = (await lancamentos.Where(x => x.DataLancamento <= hoje).ToArrayAsync(cancellationToken)).ToDictionary(x => x.Id);
        var porConta = linhas.Values.ToLookup(x => x.ContaId);
        foreach (var conta in contas)
        {
            var soma = (conta.DataSaldoInicial <= hoje ? conta.SaldoInicial : 0) + porConta[conta.Id].Where(x => x.DataLancamento >= conta.DataSaldoInicial).Sum(x =>
                x.LancamentoEstornadoId is { } id && linhas.TryGetValue(id, out var original)
                    ? -ImpactoCaixa(original.Tipo, original.FormaPagamento, original.Valor)
                    : ImpactoCaixa(x.Tipo, x.FormaPagamento, x.Valor));
            if (soma != saldos.Single(x => x.Id == conta.Id).Saldo)
                divergencias.Add($"Conta {conta.Id}: o saldo exibido diverge do lancamentos.");
        }

        return contas.Length;
    }
}
