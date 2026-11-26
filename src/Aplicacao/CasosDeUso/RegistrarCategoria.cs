using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record RegistrarCategoria(Guid UsuarioId, string Nome) : IRequest<Categoria>;
