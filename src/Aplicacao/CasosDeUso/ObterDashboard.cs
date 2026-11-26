using MediatR;

namespace Aplicacao;

public sealed record ObterDashboard(Guid UsuarioId, DateOnly Inicio, int Meses, VisaoRelatorio Visao, int Horizonte) : IRequest<Painel>;
