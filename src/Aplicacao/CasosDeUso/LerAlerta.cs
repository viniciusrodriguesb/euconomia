using MediatR;

namespace Aplicacao;

public sealed record LerAlerta(Guid UsuarioId, Guid Id) : IRequest;
