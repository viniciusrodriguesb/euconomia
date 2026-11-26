namespace Aplicacao;

public static class ConfiguracaoAlertasValidador
{
    public static void Validar(ConfigurarAlertas requisicao)
    {
        foreach (var valor in new[] { requisicao.Amarelo, requisicao.Vermelho, requisicao.Fixos, requisicao.Comprometimento, requisicao.Cartao, requisicao.Atipica })
            ValorMonetarioValidador.Validar(valor);
        if (requisicao.Vermelho < requisicao.Amarelo || requisicao.Estabelecimento < 1 || requisicao.AlertasDesativados is < 0 or > 255)
            throw new BusinessException("Informe o limite vermelho acima do amarelo e pelo menos uma transação por estabelecimento.");
    }
}
