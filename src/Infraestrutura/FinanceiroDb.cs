using System.Text.RegularExpressions;
using Dominio;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class FinanceiroDb(DbContextOptions<FinanceiroDb> opcoes)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(opcoes)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder convencoes)
    {
        convencoes.Properties<decimal>().HavePrecision(18, 2);
        convencoes.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        convencoes.Properties<string>().HaveMaxLength(240);
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);
        modelo.Entity<Usuario>().ToTable("usuario");

        ContaMapeamento.Configurar(modelo);
        CategoriaMapeamento.Configurar(modelo);
        CartaoMapeamento.Configurar(modelo);
        EstabelecimentoMapeamento.Configurar(modelo);
        AliasEstabelecimentoMapeamento.Configurar(modelo);
        CompraParceladaMapeamento.Configurar(modelo);
        FaturaMapeamento.Configurar(modelo);
        RegraRecorrenteMapeamento.Configurar(modelo);
        ExcecaoRecorrenciaMapeamento.Configurar(modelo);
        LancamentoMapeamento.Configurar(modelo);
        ConfiguracaoUsuarioMapeamento.Configurar(modelo);
        AlertaMapeamento.Configurar(modelo);
        RegistroIdempotenciaMapeamento.Configurar(modelo);
        AuditoriaMapeamento.Configurar(modelo);

        AplicarConvencoesDoModelo(modelo);
    }

    private static void AplicarConvencoesDoModelo(ModelBuilder modelo)
    {
        foreach (var entidade in modelo.Model.GetEntityTypes())
        {
            foreach (var chaveEstrangeira in entidade.GetForeignKeys())
                chaveEstrangeira.DeleteBehavior = DeleteBehavior.Restrict;

            if (entidade.ClrType.Namespace != typeof(Conta).Namespace)
                continue;

            foreach (var propriedade in entidade.GetProperties())
            {
                var coluna = propriedade.IsConcurrencyToken
                    ? "xmin"
                    : Regex.Replace(propriedade.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();

                propriedade.SetColumnName(coluna);
            }
        }
    }
}
