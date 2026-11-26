using Aplicacao;

namespace Api.Contratos;

public sealed record DadosEstorno(DateOnly Data, Guid ChaveIdempotencia, DadosLancamento? Substituto);
