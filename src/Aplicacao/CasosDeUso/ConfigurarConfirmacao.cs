using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record ConfigurarConfirmacao(Guid UsuarioId, bool ConfirmarAoVirarMes) : IRequest<ConfiguracaoUsuario>;
