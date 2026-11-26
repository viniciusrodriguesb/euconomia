using Aplicacao;
using Api.Contratos;

namespace Api.Validacoes;

internal static class RedefinirSenhaValidador
{
    public static void Validar(RedefinirSenha pedido)
    {
        CredenciaisValidador.ValidarCadastro(new(pedido.Email, pedido.Senha));

        if (pedido.Senha != pedido.ConfirmacaoSenha || pedido.Codigo is not { Length: > 0 and <= 100 })
            throw new BusinessException("Confira o código de recuperação e a confirmação da nova senha.");
    }
}
