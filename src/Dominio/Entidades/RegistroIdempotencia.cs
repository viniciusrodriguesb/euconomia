namespace Dominio;

public sealed class RegistroIdempotencia : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ChaveIdempotencia { get; set; }
    public required string ImpressaoDigital { get; set; }
    public required string Resposta { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}
