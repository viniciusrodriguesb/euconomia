namespace Api.Contratos;

public sealed record DadosPagamento(DateOnly Data, Guid ChaveIdempotencia, decimal? ValorEsperado = null);
