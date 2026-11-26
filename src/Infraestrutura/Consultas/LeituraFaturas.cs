using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class LeituraFaturas
{
    public static async Task<FaturaDetalhe[]> ListarAsync(FinanceiroDb banco, UnidadeFinanceira unidade, ListarFaturas requisicao, CancellationToken cancellationToken)
    {
        PeriodoConsultaValidador.Validar(requisicao.Inicio, requisicao.Meses, 120);
        var fim = requisicao.Inicio.AddMonths(requisicao.Meses - 1);
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var totais = Totais(banco, requisicao.UsuarioId);
        return await (from f in unidade.Ler<Fatura>(requisicao.UsuarioId)
                      join c in unidade.Ler<Cartao>(requisicao.UsuarioId) on f.CartaoId equals c.Id
                      join t in totais on f.Id equals t.FaturaId
                      where f.Competencia >= requisicao.Inicio && f.Competencia <= fim
                      orderby f.DataVencimento, c.Nome
                      select new FaturaDetalhe(f.Id, c.Id, c.Nome, f.Competencia, f.DataFechamento, f.DataVencimento,
                          f.Status == StatusFatura.Aberta && hoje > f.DataFechamento ? StatusFatura.Fechada : f.Status, t.Total, f.LancamentoPagamentoId))
            .ToArrayAsync(cancellationToken);
    }

    public static IQueryable<TotalFaturaLinha> Totais(FinanceiroDb banco, Guid usuario) => banco.Database.SqlQuery<TotalFaturaLinha>(
        $"SELECT fatura_id AS \"FaturaId\", total AS \"Total\" FROM total_fatura WHERE usuario_id = {usuario}");
}
