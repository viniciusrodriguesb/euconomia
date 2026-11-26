using Dominio;
using Microsoft.AspNetCore.Identity;

namespace Infraestrutura;

public sealed class ProvisionarUsuario(FinanceiroDb db, UserManager<Usuario> usuarios)
{
    public async Task<IdentityResult> RegistrarAsync(string email, string senha, CancellationToken ct)
    {
        await using var transacao = await db.Database.BeginTransactionAsync(ct);
        var usuario = new Usuario { Email = email, UserName = email };
        var resultado = await usuarios.CreateAsync(usuario, senha);
        if (!resultado.Succeeded)
            return resultado;

        db.Set<ConfiguracaoUsuario>().Add(new() { UsuarioId = usuario.Id });
        string[] padrao = ["Alimentação", "Moradia", "Transporte", "Saúde", "Educação", "Lazer", "Salário", "Investimentos", "Outros"];
        db.Set<Categoria>().AddRange(padrao.Select(nome => new Categoria { UsuarioId = usuario.Id, Nome = nome }));
        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return resultado;
    }
}
