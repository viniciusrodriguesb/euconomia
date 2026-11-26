using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Contratos;
using Microsoft.AspNetCore.Identity;
using Infraestrutura;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integracao;

public sealed partial class CenariosFinanceiros
{
    [Fact]
    public async Task CadastroExigeConfirmacoesNoServidor()
    {
        using var client = factory!.CreateClient();
        await Token(client);
        var cadastro = new CadastroUsuario("cadastro@example.com", "Teste!012345", "outra@example.com", "Teste!012345");
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync("/api/sessao/registrar", cadastro)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync("/api/sessao/registrar", cadastro with
                { ConfirmacaoEmail = cadastro.Email, ConfirmacaoSenha = "Diferente!012345" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync("/api/sessao/registrar", new Credenciais(cadastro.Email, cadastro.Senha))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao(cadastro.Senha))).StatusCode);
    }

    [Fact]
    public async Task RecuperacaoConsomeCodigoInvalidaSessoesEPreservaCodigoQuandoSenhaEhRejeitada()
    {
        using var autenticado = factory!.CreateClient();
        const string email = "recuperacao@example.com";
        await Entrar(autenticado, email);
        var codigo = await Post<CodigoRecuperacao>(autenticado, "/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao("Teste!012345"));
        Assert.Matches("^[A-F0-9]{64}$", codigo.Codigo);
        await using (var escopo = factory.Services.CreateAsyncScope())
        {
            var usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var usuario = await usuarios.FindByEmailAsync(email);
            var hash = await usuarios.GetAuthenticationTokenAsync(usuario!, "Euconomia", "RecuperacaoSenha");
            Assert.NotEqual(codigo.Codigo, hash);
            Assert.Matches("^[A-F0-9]{64}$", hash!);
        }

        using var anonimo = factory.CreateClient();
        await Token(anonimo);
        var pedido = new RedefinirSenha(email, codigo.Codigo, "NovaSenha!012345", "NovaSenha!012345");
        var invalido = await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido with { Codigo = new string('0', 64) });
        var inexistente = await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido with { Email = "ausente@example.com" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
        Assert.Equal(
            (await invalido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString(),
            (await inexistente.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido with { ConfirmacaoSenha = "OutraSenha!012345" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido with { Senha = "abcdefghijkl", ConfirmacaoSenha = "abcdefghijkl" })).StatusCode);
        await Ok(await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido));
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await autenticado.GetAsync("/api/sessao")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonimo.PostAsJsonAsync("/api/sessao/entrar", new Credenciais(email, "Teste!012345"))).StatusCode);
        await Ok(await anonimo.PostAsJsonAsync("/api/sessao/entrar", new Credenciais(email, pedido.Senha)));
    }

    [Fact]
    public async Task GeracaoExigeSenhaAtualERotacaoInvalidaCodigoAnterior()
    {
        using var client = factory!.CreateClient();
        const string email = "rotacao@example.com";
        await Entrar(client, email);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync("/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao("incorreta"))).StatusCode);
        var primeiro = await Post<CodigoRecuperacao>(client, "/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao("Teste!012345"));
        var segundo = await Post<CodigoRecuperacao>(client, "/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao("Teste!012345"));
        Assert.NotEqual(primeiro.Codigo, segundo.Codigo);
        using var anonimo = factory.CreateClient();
        await Token(anonimo);
        var pedido = new RedefinirSenha(email, primeiro.Codigo, "NovaSenha!012345", "NovaSenha!012345");
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido)).StatusCode);
        await Ok(await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido with { Codigo = segundo.Codigo.ToLowerInvariant() }));
    }

    [Fact]
    public async Task CodigoSoPodeSerUsadoUmaVezMesmoEmRequisicoesSimultaneas()
    {
        using var client = factory!.CreateClient();
        const string email = "concorrencia@example.com";
        await Entrar(client, email);
        var codigo = await Post<CodigoRecuperacao>(client, "/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao("Teste!012345"));
        using var primeiro = factory.CreateClient();
        using var segundo = factory.CreateClient();
        await Token(primeiro);
        await Token(segundo);
        var pedido = new RedefinirSenha(email, codigo.Codigo, "NovaSenha!012345", "NovaSenha!012345");
        var respostas = await Task.WhenAll(
            primeiro.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido),
            segundo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido));
        Assert.Single(respostas, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(respostas, x => x.StatusCode == HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RecuperacaoDesbloqueiaContaEAceitaSomenteANovaSenha()
    {
        using var client = factory!.CreateClient();
        const string email = "bloqueio@example.com";
        await Entrar(client, email);
        var codigo = await Post<CodigoRecuperacao>(client, "/api/recuperacao-conta/codigo", new GerarCodigoRecuperacao("Teste!012345"));
        using var anonimo = factory.CreateClient();
        await Token(anonimo);
        for (var tentativa = 0; tentativa < 5; tentativa++)
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonimo.PostAsJsonAsync("/api/sessao/entrar", new Credenciais(email, "Errada!012345"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonimo.PostAsJsonAsync("/api/sessao/entrar", new Credenciais(email, "Teste!012345"))).StatusCode);
        await Ok(await anonimo.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha",
            new RedefinirSenha(email, codigo.Codigo, "NovaSenha!012345", "NovaSenha!012345")));
        await Ok(await anonimo.PostAsJsonAsync("/api/sessao/entrar", new Credenciais(email, "NovaSenha!012345")));
    }

    [Fact]
    public async Task RecuperacaoExigeAntifalsificacaoELimitaTentativas()
    {
        using var client = factory!.CreateClient();
        var pedido = new RedefinirSenha("ausente@example.com", "incorreto", "NovaSenha!012345", "NovaSenha!012345");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido)).StatusCode);
        await Token(client);
        // A primeira requisição já ocupa uma tentativa, mesmo sem token.
        for (var tentativa = 0; tentativa < 9; tentativa++)
            Assert.Equal(HttpStatusCode.UnprocessableEntity,
                (await client.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido)).StatusCode);
        var limitada = await client.PostAsJsonAsync("/api/recuperacao-conta/redefinir-senha", pedido);
        Assert.Equal(HttpStatusCode.TooManyRequests, limitada.StatusCode);
        Assert.NotNull(limitada.Headers.RetryAfter);
    }
}
