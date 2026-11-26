using Dominio;

namespace Aplicacao;

public static class RegraRecorrenteValidador
{
    public static void ValidarCadastro(DadosRegra dados)
    {
        ValorMonetarioValidador.Validar(dados.ValorBase);
        if (!Enum.IsDefined(dados.Tipo) || !Enum.IsDefined(dados.FormaPagamento) || dados.DiaDoMes is < 1 or > 31 ||
            dados.CompetenciaInicio.Year < 2 || dados.CompetenciaInicio.Day != 1 || dados.CompetenciaFim is { } fim && (fim.Day != 1 || fim < dados.CompetenciaInicio) ||
            string.IsNullOrWhiteSpace(dados.Descricao) || dados.Descricao.Length > 240)
            throw new BusinessException("Confira o tipo, o dia do mês, a descrição e o período da regra.");
        var credito = dados.FormaPagamento == FormaPagamento.CartaoCredito;
        if (credito != (dados.Tipo == TipoRecorrencia.AssinaturaCartao) || credito != dados.CartaoId.HasValue)
            throw new BusinessException("Selecione um cartão para assinaturas no crédito.");
    }

    public static void ValidarCompetencia(RegraRecorrente regra, DateOnly mes)
    {
        if (mes.Day != 1 || mes < regra.CompetenciaInicio || mes > regra.CompetenciaFim)
            throw new BusinessException("Selecione um mês dentro da vigência da regra.");
    }
}
