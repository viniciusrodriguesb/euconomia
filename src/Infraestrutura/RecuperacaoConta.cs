using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Infraestrutura;

public sealed class RecuperacaoConta(
    FinanceiroDb db,
    UserManager<Usuario> usuarios,
    UnidadeFinanceira unidade)
{
    private const string Provedor = "Euconomia";
    private const string NomeToken = "RecuperacaoSenha";

    public async Task<string?> GerarAsync(Guid usuarioId, string senha, CancellationToken ct)
    {
        await using var transacao = await unidade.IniciarAsync(usuarioId, ct);
        var usuario = await usuarios.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
            return null;

        await db.Entry(usuario).ReloadAsync(ct);
        if (await usuarios.IsLockedOutAsync(usuario) || !await usuarios.CheckPasswordAsync(usuario, senha))
            return null;

        var codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var resultado = await usuarios.SetAuthenticationTokenAsync(usuario, Provedor, NomeToken, Hash(codigo));
        if (!resultado.Succeeded)
            return null;

        await transacao.CommitAsync(ct);
        return codigo;
    }

    public async Task<bool> RedefinirAsync(string email, string codigo, string senha, CancellationToken ct)
    {
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null)
            return false;

        await using var transacao = await unidade.IniciarAsync(usuario.Id, ct);
        await db.Entry(usuario).ReloadAsync(ct);
        var armazenado = await usuarios.GetAuthenticationTokenAsync(usuario, Provedor, NomeToken);
        var normalizado = codigo.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        var esperado = armazenado ?? new string('0', 64);
        var coincide = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(esperado), Encoding.ASCII.GetBytes(Hash(normalizado)));
        if (!coincide || armazenado is null)
            return false;

        var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        if (!(await usuarios.ResetPasswordAsync(usuario, token, senha)).Succeeded ||
            !(await usuarios.RemoveAuthenticationTokenAsync(usuario, Provedor, NomeToken)).Succeeded ||
            !(await usuarios.SetLockoutEndDateAsync(usuario, null)).Succeeded ||
            !(await usuarios.ResetAccessFailedCountAsync(usuario)).Succeeded)
            return false;

        await transacao.CommitAsync(ct);
        return true;
    }

    private static string Hash(string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo)));
}
