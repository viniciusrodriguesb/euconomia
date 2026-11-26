namespace Dominio;

public sealed class Auditoria : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EntidadeId { get; set; }
    public required string Entidade { get; set; }
    public required string Acao { get; set; }
    public string? Antes { get; set; }
    public string? Depois { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}
