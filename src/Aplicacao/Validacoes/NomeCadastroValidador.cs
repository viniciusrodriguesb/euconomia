namespace Aplicacao;

public static class NomeCadastroValidador
{
    public static string Validar(string nome) => !string.IsNullOrWhiteSpace(nome) && nome.Trim().Length <= 160
        ? nome.Trim() : throw new BusinessException("Informe um nome com até 160 caracteres.");
}
