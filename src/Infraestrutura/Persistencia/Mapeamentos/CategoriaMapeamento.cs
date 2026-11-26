using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class CategoriaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var categoria = MapeamentoRegistro.Registrar<Categoria>(modelo, "categoria");
        categoria.HasIndex(categoriaRegistro => new { categoriaRegistro.UsuarioId, categoriaRegistro.Nome }).IsUnique().HasFilter("inativado_em IS NULL");
    }
}
