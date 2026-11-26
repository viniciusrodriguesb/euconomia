namespace Dominio;

public sealed class CompraParcelada : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CartaoId { get; set; }
    public decimal ValorTotal { get; set; }
    public int TotalParcelas { get; set; }
    public DateOnly DataCompra { get; set; }
}
