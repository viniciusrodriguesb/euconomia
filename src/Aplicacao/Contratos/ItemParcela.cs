namespace Aplicacao;

public sealed record ItemParcela(Guid Id, int Numero, DateOnly Fatura, decimal Valor, bool Paga, bool Estornada);
