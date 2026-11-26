namespace Api.Contratos;

public sealed record CadastroUsuario(string Email, string Senha, string ConfirmacaoEmail, string ConfirmacaoSenha);
