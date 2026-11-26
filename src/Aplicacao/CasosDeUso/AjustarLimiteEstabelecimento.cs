using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record AjustarLimiteEstabelecimento(Guid UsuarioId, Guid Id, int? Limite) : IRequest<Estabelecimento>;
