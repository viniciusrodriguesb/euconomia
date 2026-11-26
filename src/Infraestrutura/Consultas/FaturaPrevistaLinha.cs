namespace Infraestrutura;

internal sealed record FaturaPrevistaLinha(Guid Id, DateOnly Vencimento, decimal Total);
