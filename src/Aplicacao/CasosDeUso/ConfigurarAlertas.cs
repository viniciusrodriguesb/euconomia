using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record ConfigurarAlertas(Guid UsuarioId, decimal Amarelo, decimal Vermelho, decimal Fixos,
    decimal Comprometimento, decimal Cartao, int Estabelecimento, decimal Atipica, int AlertasDesativados) : IRequest<ConfiguracaoUsuario>;
