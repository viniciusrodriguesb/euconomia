using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record AjustarRecorrencia(Guid UsuarioId, Guid Id, uint VersaoRegra, DateOnly Competencia,
    uint? VersaoExcecao, decimal? Valor, bool Ignorada, bool Restaurar) : IRequest<ExcecaoRecorrencia?>;
