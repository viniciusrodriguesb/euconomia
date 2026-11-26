using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class CartaoMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var cartao = MapeamentoRegistro.Registrar<Cartao>(modelo, "cartao");
        MapeamentoRegistro.Vincular<Cartao, Conta>(cartao, nameof(Cartao.ContaPagamentoId));
        cartao.Property(cartaoRegistro => cartaoRegistro.Versao).IsRowVersion();
        MapeamentoRegistro.AdicionarRestricao(cartao, "limite_dias", "limite > 0 AND dia_fechamento BETWEEN 1 AND 31 AND dia_vencimento BETWEEN 1 AND 31");
    }
}
