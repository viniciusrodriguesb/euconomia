namespace Aplicacao;

public static class PeriodoConsultaValidador
{
    public static void Validar(DateOnly mes, int quantidade, int limite)
    {
        if (mes.Day != 1 || quantidade < 1 || quantidade > limite || mes.Year > 9989)
            throw new BusinessException("Informe o primeiro dia do mês e um período válido.");
    }
}
