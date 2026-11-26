using Microsoft.AspNetCore.Identity;

namespace Infraestrutura;

public sealed class Usuario : IdentityUser<Guid>
{
    public Usuario() => Id = Guid.CreateVersion7();
}
