using MediatR;

namespace Aplicacao;

public sealed record ListarParcelas(Guid UsuarioId, DateOnly Mes) : IRequest<CompraDetalhe[]>;
