namespace Api.Contratos;

public sealed record RedefinirSenha(string Email, string Codigo, string Senha, string ConfirmacaoSenha);
