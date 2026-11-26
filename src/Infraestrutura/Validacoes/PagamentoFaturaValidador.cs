using Aplicacao;
using Dominio;

namespace Infraestrutura;

internal static class PagamentoFaturaValidador
{
    public static void ValidarData(PagarFatura requisicao, Fatura fatura, Conta conta)
    {
        if (fatura.Status == StatusFatura.Paga)
            throw new BusinessException("A fatura já foi paga. Atualize a lista de faturas.");
        if (requisicao.Data < fatura.DataFechamento || requisicao.Data < conta.DataSaldoInicial || requisicao.Data > DateOnly.FromDateTime(DateTime.Now))
            throw new BusinessException("Informe uma data de pagamento entre o fechamento da fatura e hoje, dentro do período da conta.");
    }

    public static void ValidarTotal(decimal total, decimal? valorEsperado)
    {
        ValorMonetarioValidador.Validar(total);
        if (valorEsperado is { } esperado && esperado != total)
            throw new ConcorrenciaException();
    }
}
