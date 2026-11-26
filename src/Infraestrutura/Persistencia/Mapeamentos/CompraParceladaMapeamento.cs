using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class CompraParceladaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var compra = MapeamentoRegistro.Registrar<CompraParcelada>(modelo, "compra_parcelada");
        MapeamentoRegistro.Vincular<CompraParcelada, Cartao>(compra, nameof(CompraParcelada.CartaoId));
        MapeamentoRegistro.AdicionarRestricao(compra, "valor_parcelas", "total_parcelas >= 2 AND valor_total >= total_parcelas * 0.01");
    }
}
