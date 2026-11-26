namespace Api.Contratos;

public sealed record DadosAlertas(decimal Amarelo, decimal Vermelho, decimal Fixos, decimal Comprometimento, decimal Cartao,
    int Estabelecimento, decimal Atipica, int AlertasDesativados);
