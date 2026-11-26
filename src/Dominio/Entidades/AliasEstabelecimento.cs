namespace Dominio;

public sealed class AliasEstabelecimento : IRegistroUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EstabelecimentoId { get; set; }
    public required string NomeNormalizado { get; set; }
}
