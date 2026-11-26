namespace Aplicacao;

public static class CartaoValidador
{
    public static void Validar(DadosCartao dados)
    {
        ValorMonetarioValidador.Validar(dados.Limite);
        if (dados.DiaFechamento is < 1 or > 31 || dados.DiaVencimento is < 1 or > 31)
            throw new BusinessException("Informe os dias de fechamento e vencimento entre 1 e 31.");
    }
}
