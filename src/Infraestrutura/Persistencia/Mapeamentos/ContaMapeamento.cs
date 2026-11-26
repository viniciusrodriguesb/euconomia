using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class ContaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var conta = MapeamentoRegistro.Registrar<Conta>(modelo, "conta");
        conta.HasIndex(contaRegistro => new { contaRegistro.UsuarioId, contaRegistro.Nome }).IsUnique().HasFilter("inativado_em IS NULL");
    }
}
