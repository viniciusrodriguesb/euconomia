using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class LeituraContabil
{
    public static async Task<SaldoConta[]> SaldosAsync(UnidadeFinanceira unidade, Guid usuario, DateOnly hoje, CancellationToken cancellationToken)
    {
        var movimentos = Movimentos(unidade, usuario);
        return await unidade.Ler<Conta>(usuario).Select(c => new SaldoConta(c.Id, (c.DataSaldoInicial <= hoje ? c.SaldoInicial : 0) +
            movimentos.Where(x => x.ContaId == c.Id && !x.Credito && x.Data >= c.DataSaldoInicial && x.Data <= hoje)
                .Sum(x => x.Tipo == TipoLancamento.Receita || x.Tipo == TipoLancamento.ResgateInvestimento ? x.Valor : -x.Valor)))
            .ToArrayAsync(cancellationToken);
    }

    public static IQueryable<Movimento> Movimentos(UnidadeFinanceira unidade, Guid usuario)
    {
        var linhas = unidade.Ler<Lancamento>(usuario);
        return from l in linhas
               join e in linhas on l.LancamentoEstornadoId equals e.Id into originais
               from original in originais.DefaultIfEmpty()
               select new Movimento
               {
                   ContaId = l.ContaId,
                   CategoriaId = l.CategoriaId,
                   EstabelecimentoId = l.EstabelecimentoId,
                   NumeroParcela = original == null ? l.NumeroParcela : original.NumeroParcela,
                   Competencia = l.Competencia,
                   Data = l.DataLancamento,
                   Tipo = original == null ? l.Tipo : original.Tipo,
                   Credito = l.FormaPagamento == FormaPagamento.CartaoCredito,
                   Fixo = original == null ? l.RegraRecorrenteId != null : original.RegraRecorrenteId != null,
                   RegraId = original == null ? l.RegraRecorrenteId : original.RegraRecorrenteId,
                   Valor = original == null ? l.Valor : -l.Valor
               };
    }
}
