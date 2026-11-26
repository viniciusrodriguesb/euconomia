using Dominio;

namespace Aplicacao;

public sealed record ResumoMes(DateOnly Mes, decimal Entradas, decimal Despesas, decimal Fixos, decimal Cartao,
    decimal Aportes, decimal Resgates, decimal Retiradas, decimal Pagamentos, decimal VariacaoCaixa, decimal FixosCartao);
