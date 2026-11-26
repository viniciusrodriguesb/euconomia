using System.Globalization;
using System.Text;

namespace Aplicacao;

public static class EstabelecimentoValidador
{
    public static (string Nome, string NomeNormalizado) ValidarCadastro(DadosEstabelecimento dados)
    {
        var nome = NomeCadastroValidador.Validar(dados.Nome);
        if (dados.LimiteTransacoes is <= 0)
            throw new BusinessException("Informe um limite de transações maior que zero.");

        return (nome, Normalizar(nome, "Informe um nome com letras ou números."));
    }

    public static string ValidarAlias(string nome) =>
        Normalizar(NomeCadastroValidador.Validar(nome), "Informe um alias com letras ou números.");

    public static void ValidarLimite(int? limite)
    {
        if (limite is <= 0)
            throw new BusinessException("Informe um limite maior que zero ou limpe o campo para usar o padrão.");
    }

    private static string Normalizar(string nome, string mensagem)
    {
        var caracteres = nome.Normalize(NormalizationForm.FormD)
            .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            .Select(caractere => char.IsLetterOrDigit(caractere) ? char.ToUpperInvariant(caractere) : ' ')
            .ToArray();
        var normalizado = string.Join(' ', new string(caracteres).Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return normalizado.Length > 0 ? normalizado : throw new BusinessException(mensagem);
    }
}
