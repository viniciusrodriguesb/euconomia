namespace Dominio;

public sealed class ExcecaoRecorrencia : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RegraRecorrenteId { get; set; }
    public DateOnly Competencia { get; set; }
    public decimal? ValorOverride { get; set; }
    public bool Ignorada { get; set; }
    public uint Versao { get; set; }
}
