using System.Net.Mail;
using Aplicacao;
using Api.Contratos;

namespace Api.Validacoes;

internal static class CredenciaisValidador
{
    public static void ValidarCadastro(Credenciais credenciais)
    {
        var email = credenciais.Email;
        if (email is not { Length: > 0 and <= 240 } ||
            !MailAddress.TryCreate(email, out var endereco) || endereco.Address != email ||
            credenciais.Senha is not { Length: >= 12 and <= 128 })
        {
            throw new BusinessException("Informe um e-mail válido e uma senha de 12 a 128 caracteres.");
        }
    }

    public static void ValidarEntrada(Credenciais credenciais)
    {
        if (string.IsNullOrWhiteSpace(credenciais.Email) || credenciais.Email.Length > 240 ||
            string.IsNullOrEmpty(credenciais.Senha) || credenciais.Senha.Length > 128)
        {
            throw new BusinessException("Informe e-mail e senha válidos.");
        }
    }
}
