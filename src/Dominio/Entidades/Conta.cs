namespace Dominio;

public sealed class Conta : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Nome { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateOnly DataSaldoInicial { get; set; }
    public DateTime? InativadoEm { get; set; }
}
