using Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestrutura;

internal static class MapeamentoRegistro
{
    public static EntityTypeBuilder<T> Registrar<T>(ModelBuilder modelo, string tabela)
        where T : class, IRegistroUsuario
    {
        var registro = modelo.Entity<T>().ToTable(tabela);
        registro.HasKey(entidade => new { entidade.UsuarioId, entidade.Id });
        registro.HasOne<Usuario>().WithMany().HasForeignKey(entidade => entidade.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        return registro;
    }

    public static void Vincular<T, TDestino>(EntityTypeBuilder<T> entidade, string propriedade)
        where T : class, IRegistroUsuario
        where TDestino : class, IRegistroUsuario =>
        entidade.HasOne<TDestino>().WithMany()
            .HasForeignKey(nameof(IRegistroUsuario.UsuarioId), propriedade)
            .OnDelete(DeleteBehavior.Restrict);

    public static void AdicionarRestricao<T>(EntityTypeBuilder<T> entidade, string nome, string sql) where T : class =>
        entidade.ToTable(tabela => tabela.HasCheckConstraint($"ck_{entidade.Metadata.GetTableName()}_{nome}", sql));
}
