namespace Dominio;

public sealed class RegraRecorrente : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? RegraAnteriorId { get; set; }
    public required string Descricao { get; set; }
    public TipoRecorrencia Tipo { get; set; }
    public decimal ValorBase { get; set; }
    public int DiaDoMes { get; set; }
    public DateOnly CompetenciaInicio { get; set; }
    public DateOnly? CompetenciaFim { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public Guid ContaId { get; set; }
    public Guid? CartaoId { get; set; }
    public Guid CategoriaId { get; set; }
    public uint Versao { get; set; }
}
