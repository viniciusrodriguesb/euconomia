namespace Aplicacao;

public sealed record EfeitoLancamento(decimal ImpactoNoSaldo, DateOnly Competencia, IReadOnlyList<ParcelaPrevista> Faturas);
