namespace Aplicacao;

public static class ExcecaoRecorrenciaValidador
{
    public static void Validar(AjustarRecorrencia requisicao)
    {
        if (requisicao.Restaurar && (requisicao.Ignorada || requisicao.Valor.HasValue) ||
            !requisicao.Restaurar && (requisicao.Ignorada == requisicao.Valor.HasValue))
            throw new BusinessException("Escolha entre ajustar o valor, pular o mês ou voltar ao padrão.");
        if (requisicao.Valor is { } valor)
            ValorMonetarioValidador.Validar(valor);
    }
}
