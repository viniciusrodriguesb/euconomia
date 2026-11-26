namespace Aplicacao;

public static class ValorMonetarioValidador
{
    public static void Validar(decimal valor)
    {
        if (valor <= 0 || valor > 9_999_999_999_999_999.99m || decimal.Round(valor, 2) != valor)
            throw new BusinessException("Informe um valor positivo, dentro do limite e com até duas casas decimais.");
    }
}
