namespace Dominio;

public sealed class Cartao : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Nome { get; set; }
    public required string Bandeira { get; set; }
    public decimal Limite { get; set; }
    public int DiaFechamento { get; set; }
    public int DiaVencimento { get; set; }
    public Guid ContaPagamentoId { get; set; }
    public DateTime? InativadoEm { get; set; }
    public uint Versao { get; set; }
}
