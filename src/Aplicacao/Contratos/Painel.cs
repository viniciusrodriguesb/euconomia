namespace Aplicacao;

public sealed record Painel(ResumoMes[] Periodo, ProjecaoMes[] Projecao, decimal SaldoAtual, decimal PatrimonioInvestido,
    decimal SaldoProjetadoFimMes, ResumoMes MesAnterior, decimal MediaEntradas6Meses, decimal MediaSaidas6Meses,
    Ranking[] Categorias, Ranking[] Estabelecimentos, FaturaDetalhe[] Faturas, OcorrenciaPrevista[] Previstos,
    AlertaExibido[] Alertas, string? AvisoPercentuais, PatrimonioMes[] PatrimonioPorMes, RealizadoRegra[] ValoresRealizados,
    EvolucaoMes[] Evolucao);
