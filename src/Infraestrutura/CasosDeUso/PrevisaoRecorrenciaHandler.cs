using Aplicacao;
using MediatR;

namespace Infraestrutura;

public sealed class PrevisaoRecorrenciaHandler(UnidadeFinanceira unidade)
    : IRequestHandler<ListarPrevisao, OcorrenciaPrevista[]>
{
    public Task<OcorrenciaPrevista[]> Handle(ListarPrevisao requisicao, CancellationToken cancellationToken) =>
        PrevisaoRecorrencias.ListarAsync(unidade, requisicao, cancellationToken);
}
