using Aplicacao;
using Api.Contratos;

namespace Api.Validacoes;

internal static class CadastroUsuarioValidador
{
    public static void Validar(CadastroUsuario cadastro)
    {
        CredenciaisValidador.ValidarCadastro(new(cadastro.Email, cadastro.Senha));

        if (!string.Equals(cadastro.Email, cadastro.ConfirmacaoEmail, StringComparison.OrdinalIgnoreCase) ||
            cadastro.Senha != cadastro.ConfirmacaoSenha)
            throw new BusinessException("A confirmação do e-mail e da senha deve corresponder aos dados informados.");
    }
}
