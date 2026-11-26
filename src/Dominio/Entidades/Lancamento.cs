namespace Dominio;

public sealed class Lancamento : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public TipoLancamento Tipo { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public decimal Valor { get; set; }
    public string Moeda { get; set; } = "BRL";
    public DateOnly DataLancamento { get; set; }
    public DateOnly Competencia { get; set; }
    public Guid ContaId { get; set; }
    public Guid? CartaoId { get; set; }
    public Guid? FaturaId { get; set; }
    public Guid? CategoriaId { get; set; }
    public Guid? EstabelecimentoId { get; set; }
    public required string Descricao { get; set; }
    public Guid? CompraParceladaId { get; set; }
    public int? NumeroParcela { get; set; }
    public int? TotalParcelas { get; set; }
    public Guid? RegraRecorrenteId { get; set; }
    public Guid? LancamentoEstornadoId { get; set; }
    public Guid ChaveIdempotencia { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}
