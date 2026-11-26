using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Infraestrutura;

public sealed class ListarFaturasHandler(FinanceiroDb banco, UnidadeFinanceira unidade) : IRequestHandler<ListarFaturas, FaturaDetalhe[]>
{
    public Task<FaturaDetalhe[]> Handle(ListarFaturas requisicao, CancellationToken cancellationToken) =>
        LeituraFaturas.ListarAsync(banco, unidade, requisicao, cancellationToken);
}
