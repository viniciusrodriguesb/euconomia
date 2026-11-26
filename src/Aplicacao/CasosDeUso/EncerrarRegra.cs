using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record EncerrarRegra(Guid UsuarioId, Guid Id, uint Versao, DateOnly Competencia) : IRequest<RegraRecorrente>;
