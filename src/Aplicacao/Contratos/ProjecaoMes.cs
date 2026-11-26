namespace Aplicacao;

public sealed record ProjecaoMes(DateOnly Mes, decimal Entradas, decimal Fixos, decimal ParcelasEFaturas, decimal Aportes,
    decimal SaldoAoFim, bool Ajustado);
