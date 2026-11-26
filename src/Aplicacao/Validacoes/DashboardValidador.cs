namespace Aplicacao;

public static class DashboardValidador
{
    public static void Validar(ObterDashboard requisicao)
    {
        PeriodoConsultaValidador.Validar(requisicao.Inicio, requisicao.Meses, 12);

        if (!Enum.IsDefined(requisicao.Visao) || requisicao.Horizonte is < 1 or > 60 || requisicao.Inicio.Year < 2)
            throw new BusinessException("Informe uma visão e um horizonte de projeção válidos.");
    }
}
