using MediatR;

namespace Aplicacao;

public sealed record MesclarEstabelecimento(Guid UsuarioId, Guid Origem, Guid Destino) : IRequest;
