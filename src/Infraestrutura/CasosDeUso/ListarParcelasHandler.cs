using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Infraestrutura;

public sealed class ListarParcelasHandler(UnidadeFinanceira unidade) : IRequestHandler<ListarParcelas, CompraDetalhe[]>
{
    public async Task<CompraDetalhe[]> Handle(ListarParcelas requisicao, CancellationToken cancellationToken)
    {
        var lancamentos = unidade.Ler<Lancamento>(requisicao.UsuarioId);
        var linhas = await (from l in lancamentos
                            join compra in unidade.Ler<CompraParcelada>(requisicao.UsuarioId) on l.CompraParceladaId equals compra.Id
                            join f in unidade.Ler<Fatura>(requisicao.UsuarioId) on l.FaturaId equals f.Id
                            join c in unidade.Ler<Cartao>(requisicao.UsuarioId) on f.CartaoId equals c.Id
                            orderby l.NumeroParcela
                            select new
                            {
                                Compra = compra,
                                c.Nome,
                                l.Descricao,
                                Linha = new ItemParcela(l.Id, l.NumeroParcela!.Value,
                                f.Competencia, l.Valor, f.Status == StatusFatura.Paga, lancamentos.Any(e => e.LancamentoEstornadoId == l.Id))
                            })
            .ToArrayAsync(cancellationToken);
        return linhas.GroupBy(x => x.Compra.Id).Select(g =>
        {
            var primeira = g.First();
            var parcelas = g.Select(x => x.Linha).ToArray();
            var atual = parcelas.LastOrDefault(x => x.Fatura <= requisicao.Mes) ?? parcelas[0];
            return new CompraDetalhe(g.Key, primeira.Compra.CartaoId, primeira.Nome, primeira.Compra.DataCompra, primeira.Descricao,
                atual.Numero, primeira.Compra.TotalParcelas, atual.Valor, parcelas.Where(x => !x.Paga && !x.Estornada).Sum(x => x.Valor),
                parcelas[^1].Fatura, parcelas);
        }).OrderByDescending(x => x.Compra).ToArray();
    }
}
