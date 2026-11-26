using Dominio;

namespace Aplicacao;

public sealed record CompraDetalhe(Guid Id, Guid CartaoId, string Cartao, DateOnly Compra, string Descricao, int Atual,
    int TotalParcelas, decimal ValorParcela, decimal FaltaPagar, DateOnly TerminaEm, ItemParcela[] Parcelas);
