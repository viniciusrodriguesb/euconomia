namespace Dominio;

public sealed class Alerta : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public TipoAlerta Tipo { get; set; }
    public GravidadeAlerta Gravidade { get; set; }
    public DateOnly Competencia { get; set; }
    public required string Mensagem { get; set; }
    public required string Referencia { get; set; }
    public DateTime? LidoEm { get; set; }
}
