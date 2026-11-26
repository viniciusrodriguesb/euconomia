using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Infraestrutura;

public sealed class ListarLancamentosHandler(UnidadeFinanceira unidade) : IRequestHandler<ListarLancamentos, Pagina<Lancamento>>
{
    public Task<Pagina<Lancamento>> Handle(ListarLancamentos requisicao, CancellationToken cancellationToken)
    {
        var consulta = unidade.Ler<Lancamento>(requisicao.UsuarioId);
        if (requisicao.Competencia is { } mes)
            consulta = consulta.Where(x => x.Competencia == mes);
        return consulta.OrderByDescending(x => x.DataLancamento).ThenByDescending(x => x.Id)
            .PaginarAsync(requisicao.Pagina, requisicao.Tamanho, cancellationToken);
    }
}
