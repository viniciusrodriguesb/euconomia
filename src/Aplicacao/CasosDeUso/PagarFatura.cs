using MediatR;

namespace Aplicacao;

public sealed record PagarFatura(Guid UsuarioId, Guid FaturaId, DateOnly Data, Guid ChaveIdempotencia, decimal? ValorEsperado = null) : IRequest<ResultadoLancamento>;
