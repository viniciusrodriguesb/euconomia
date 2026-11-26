using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record RegistrarRegra(Guid UsuarioId, DadosRegra Dados) : IRequest<RegraRecorrente>;
