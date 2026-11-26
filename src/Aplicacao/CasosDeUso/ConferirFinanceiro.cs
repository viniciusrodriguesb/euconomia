using MediatR;

namespace Aplicacao;

public sealed record ConferirFinanceiro(Guid UsuarioId) : IRequest<ResultadoConferencia>;
