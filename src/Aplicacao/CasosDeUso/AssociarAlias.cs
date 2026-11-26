using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record AssociarAlias(Guid UsuarioId, Guid Id, string Nome) : IRequest<AliasEstabelecimento>;
