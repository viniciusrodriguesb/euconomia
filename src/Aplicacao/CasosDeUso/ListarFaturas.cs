using MediatR;

namespace Aplicacao;

public sealed record ListarFaturas(Guid UsuarioId, DateOnly Inicio, int Meses) : IRequest<FaturaDetalhe[]>;
