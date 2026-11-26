namespace Dominio;

public sealed class Fatura : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CartaoId { get; set; }
    public DateOnly Competencia { get; set; }
    public DateOnly DataFechamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public StatusFatura Status { get; set; } = StatusFatura.Aberta;
    public Guid? LancamentoPagamentoId { get; set; }
    public uint Versao { get; set; }
}
