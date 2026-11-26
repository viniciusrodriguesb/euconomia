using MediatR;

namespace Aplicacao;

public sealed record ListarPrevisao(Guid UsuarioId, DateOnly Inicio, int Meses) : IRequest<OcorrenciaPrevista[]>;
