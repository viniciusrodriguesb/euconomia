using System.Net.Http.Json;
using Api.Contratos;
using Infraestrutura;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integracao;

public sealed partial class CenariosFinanceiros
{
    [Fact]
    public async Task RenomeacaoPreservaCodigoDeRecuperacaoExistente()
    {
        using var client = factory!.CreateClient();
        const string email = "marca@example.com";
        await Entrar(client, email);
        var codigo = await Post<CodigoRecuperacao>(client, "/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao("Teste!012345"));
        await using (var escopo = factory.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<FinanceiroDb>();
            var migrador = db.GetService<IMigrator>();
            await migrador.MigrateAsync("20261116232035_PreferenciasERecorrencias");
        }

        await using (var escopo = factory.Services.CreateAsyncScope())
        {
            var usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var usuario = await usuarios.FindByEmailAsync(email);
            Assert.NotNull(await usuarios.GetAuthenticationTokenAsync(usuario!, "Prumo", "RecuperacaoSenha"));
            var db = escopo.ServiceProvider.GetRequiredService<FinanceiroDb>();
            await db.Database.MigrateAsync();
        }

        using var anonimo = factory.CreateClient();
        await Token(anonimo);
        await Ok(await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha",
            new RedefinirSenha(email, codigo.Codigo, "NovaSenha!012345", "NovaSenha!012345")));
    }
}
