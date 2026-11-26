using Dominio;

namespace Infraestrutura;

internal sealed class Movimento
{
    public Guid ContaId { get; init; }
    public Guid? CategoriaId { get; init; }
    public Guid? EstabelecimentoId { get; init; }
    public int? NumeroParcela { get; init; }
    public DateOnly Competencia { get; init; }
    public DateOnly Data { get; init; }
    public TipoLancamento Tipo { get; init; }
    public bool Credito { get; init; }
    public bool Fixo { get; init; }
    public Guid? RegraId { get; init; }
    public decimal Valor { get; init; }
}
