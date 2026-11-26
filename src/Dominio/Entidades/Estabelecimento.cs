namespace Dominio;

public sealed class Estabelecimento : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Nome { get; set; }
    public required string NomeNormalizado { get; set; }
    public int? LimiteTransacoes { get; set; }
    public Guid? MescladoEmId { get; set; }
}
