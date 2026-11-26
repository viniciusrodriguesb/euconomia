using Dominio;

namespace Aplicacao;

public sealed record FaturaDetalhe(Guid Id, Guid CartaoId, string Cartao, DateOnly Competencia, DateOnly DataFechamento,
    DateOnly DataVencimento, StatusFatura Status, decimal Total, Guid? LancamentoPagamentoId);
