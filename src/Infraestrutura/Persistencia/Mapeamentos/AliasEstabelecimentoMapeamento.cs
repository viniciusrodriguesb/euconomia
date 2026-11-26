using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class AliasEstabelecimentoMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var alias = MapeamentoRegistro.Registrar<AliasEstabelecimento>(modelo, "alias_estabelecimento");
        alias.HasIndex(aliasRegistro => new { aliasRegistro.UsuarioId, aliasRegistro.NomeNormalizado }).IsUnique();
        MapeamentoRegistro.Vincular<AliasEstabelecimento, Estabelecimento>(alias, nameof(AliasEstabelecimento.EstabelecimentoId));
    }
}
