namespace Aplicacao;

public static class ContaValidador
{
    public static void Validar(DadosConta dados)
    {
        if (dados.SaldoInicial != 0)
            ValorMonetarioValidador.Validar(Math.Abs(dados.SaldoInicial));
        if (dados.DataSaldoInicial == default || dados.DataSaldoInicial > DateOnly.FromDateTime(DateTime.Now))
            throw new BusinessException("Informe uma data para o saldo inicial até hoje.");
    }
}
