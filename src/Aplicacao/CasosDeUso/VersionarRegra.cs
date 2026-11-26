using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record VersionarRegra(Guid UsuarioId, Guid Id, uint Versao, DateOnly Competencia, decimal Valor) : IRequest<RegraRecorrente>;
