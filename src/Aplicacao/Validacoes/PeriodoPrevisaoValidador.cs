namespace Aplicacao;

public static class PeriodoPrevisaoValidador
{
    public static DateOnly CalcularFim(DateOnly inicio, int meses)
    {
        if (inicio.Day != 1 || meses <= 0 || meses - 1 > (9999 - inicio.Year) * 12 + 12 - inicio.Month)
            throw new BusinessException("Informe uma competência inicial e um horizonte que caiba no calendário.");

        return inicio.AddMonths(meses - 1);
    }
}
