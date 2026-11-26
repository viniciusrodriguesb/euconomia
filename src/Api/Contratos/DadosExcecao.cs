namespace Api.Contratos;

public sealed record DadosExcecao(uint VersaoRegra, DateOnly Competencia, uint? VersaoExcecao, decimal? Valor, bool Ignorada, bool Restaurar);
