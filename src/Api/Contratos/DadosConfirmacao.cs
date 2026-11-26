using Aplicacao;

namespace Api.Contratos;

public sealed record DadosConfirmacao(DateOnly Competencia, Guid ChaveIdempotencia, EfeitoLancamento? EfeitoEsperado = null);
