using Dominio;

namespace Aplicacao;

public static class CalculoFinanceiro
{
    public static DateOnly Mes(DateOnly data) => new(data.Year, data.Month, 1);

    public static DateOnly Dia(DateOnly mes, int dia) =>
        new(mes.Year, mes.Month, Math.Min(dia, DateTime.DaysInMonth(mes.Year, mes.Month)));

    public static decimal[] Parcelar(decimal valor, int quantidade)
    {
        ValorMonetarioValidador.Validar(valor);
        if (quantidade < 1 || quantidade > 120 || valor < quantidade * 0.01m)
            throw new BusinessException("Informe de 1 a 120 parcelas, com pelo menos um centavo em cada parcela.");

        var normal = decimal.Round(valor / quantidade, 2, MidpointRounding.AwayFromZero);
        if (valor - normal * (quantidade - 1) < 0.01m)
            normal = decimal.Floor(valor * 100 / quantidade) / 100;

        var parcelas = new decimal[quantidade];
        Array.Fill(parcelas, normal);
        parcelas[^1] = valor - normal * (quantidade - 1);
        return parcelas;
    }

    public static ParcelaPrevista[] PreverFaturas(DateOnly data, Cartao cartao, decimal valor, int quantidade)
    {
        var mesCompra = Mes(data);
        var primeiroMes = data > Dia(mesCompra, cartao.DiaFechamento) ? mesCompra.AddMonths(1) : mesCompra;
        return Parcelar(valor, quantidade).Select((parcela, indice) =>
        {
            var mes = primeiroMes.AddMonths(indice);
            var fechamento = Dia(mes, cartao.DiaFechamento);
            var vencimento = Dia(mes, cartao.DiaVencimento);
            if (vencimento <= fechamento)
                vencimento = Dia(mes.AddMonths(1), cartao.DiaVencimento);
            return new ParcelaPrevista(indice + 1, parcela, mes, fechamento, vencimento);
        }).ToArray();
    }

    public static decimal ImpactoCaixa(TipoLancamento tipo, FormaPagamento forma, decimal valor) =>
        forma == FormaPagamento.CartaoCredito ? 0 : tipo switch
        {
            TipoLancamento.Receita or TipoLancamento.ResgateInvestimento => valor,
            TipoLancamento.Despesa or TipoLancamento.AporteInvestimento or TipoLancamento.RetiradaSaldo or TipoLancamento.PagamentoFatura => -valor,
            _ => 0
        };
}
