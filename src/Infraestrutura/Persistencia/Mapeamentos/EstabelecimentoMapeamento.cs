using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class EstabelecimentoMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var estabelecimento = MapeamentoRegistro.Registrar<Estabelecimento>(modelo, "estabelecimento");
        estabelecimento.HasIndex(estabelecimentoRegistro => new { estabelecimentoRegistro.UsuarioId, estabelecimentoRegistro.NomeNormalizado }).IsUnique();
        MapeamentoRegistro.Vincular<Estabelecimento, Estabelecimento>(estabelecimento, nameof(Estabelecimento.MescladoEmId));
        MapeamentoRegistro.AdicionarRestricao(estabelecimento, "limite", "limite_transacoes > 0");
    }
}
