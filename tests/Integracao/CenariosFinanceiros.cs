using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aplicacao;
using Api.Contratos;
using Api.Servicos;
using Dominio;
using Infraestrutura;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
using static Aplicacao.CalculoFinanceiro;

namespace Integracao;

public sealed partial class CenariosFinanceiros : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
    private readonly string banco = $"teste_euconomia_{Guid.NewGuid():N}";
    private readonly string configuracao = Environment.GetEnvironmentVariable("ConnectionStrings__Financeiro")
        ?? throw new InvalidOperationException("Configure ConnectionStrings__Financeiro para um PostgreSQL de desenvolvimento.");
    private NpgsqlConnection? administracao;
    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        var config = new NpgsqlConnectionStringBuilder(configuracao) { Database = "postgres", Pooling = false };
        administracao = new NpgsqlConnection(config.ConnectionString);
        await administracao.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {banco}", administracao).ExecuteNonQueryAsync();
        config.Database = banco;
        Environment.SetEnvironmentVariable("ConnectionStrings__Financeiro", config.ConnectionString);
        factory = new WebApplicationFactory<Program>();
        await using var escopo = factory.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<FinanceiroDb>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
            await factory.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__Financeiro", configuracao);
        if (administracao?.State == System.Data.ConnectionState.Open)
            await new NpgsqlCommand($"DROP DATABASE IF EXISTS {banco} WITH (FORCE)", administracao).ExecuteNonQueryAsync();
        if (administracao is not null)
            await administracao.DisposeAsync();
    }

    [Fact]
    public async Task FluxoRealValidaOsCriteriosFinanceirosEIsolamento()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var mes = Mes(hoje).AddMonths(-1);
        using var client = factory!.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/cadastros")).StatusCode);
        var semToken = await client.PostAsJsonAsync("/api/sessao/registrar", new Credenciais("teste@example.com", "Teste!012345"));
        Assert.Equal(HttpStatusCode.BadRequest, semToken.StatusCode);
        await Entrar(client, "teste@example.com");
        await ConferirProblemaAsync(await client.GetAsync("/api/faturas"), HttpStatusCode.BadRequest);
        await ConferirProblemaAsync(await client.GetAsync("/api/rota-inexistente"), HttpStatusCode.NotFound);
        var cadastros = await Get<Cadastros>(client, "/api/cadastros");
        Assert.NotEmpty(cadastros.Categorias);
        var categoria = cadastros.Categorias[0].Id;
        var conta = await Post<Conta>(client, "/api/contas", new DadosConta("Principal", 5000, mes.AddMonths(-2)));
        var cartao = await Post<Cartao>(client, "/api/cartoes", new DadosCartao("Crédito", "Visa", 5000, 10, 20, conta.Id));
        var loja = await Post<Estabelecimento>(client, "/api/estabelecimentos", new DadosEstabelecimento("iFood", null));
        var variante = await Post<Estabelecimento>(client, "/api/estabelecimentos", new DadosEstabelecimento("IFOOD Restaurante", null));
        var alias = await Post<AliasEstabelecimento>(client, $"/api/estabelecimentos/{loja.Id}/aliases", new NomeCadastro("iFood Online"));
        Assert.Equal(loja.Id, alias.EstabelecimentoId);
        Assert.Equal(loja.Id, (await Post<Estabelecimento>(client, "/api/estabelecimentos", new DadosEstabelecimento("IFOOD * ONLINE", null))).Id);
        var regra = await Post<RegraRecorrente>(client, "/api/recorrencias", new DadosRegra(TipoRecorrencia.AporteFixo,
            1000, 5, mes.AddMonths(-1), null, FormaPagamento.Pix, conta.Id, categoria, null, "Aporte fixo"));
        await Ok(await client.PutAsJsonAsync($"/api/recorrencias/{regra.Id}/excecao",
            new DadosExcecao(regra.Versao, mes, null, 2000, false, false), Json));
        var previsao = await Get<OcorrenciaPrevista[]>(client, $"/api/recorrencias/previsao?inicio={mes.AddMonths(-1):yyyy-MM-dd}&meses=60");
        Assert.Equal(60, previsao.Length);
        var urlPaginada = $"/api/recorrencias/previsao/paginada?inicio={mes.AddMonths(-1):yyyy-MM-dd}&meses=60";
        var primeiraPagina = await Get<Pagina<OcorrenciaPrevista>>(client, urlPaginada);
        Assert.Equal(60, primeiraPagina.Total);
        Assert.Equal(previsao.Take(10), primeiraPagina.Itens);
        var segundaPagina = await Get<Pagina<OcorrenciaPrevista>>(client, urlPaginada + "&pagina=2&tamanho=20");
        Assert.Equal(2, segundaPagina.PaginaAtual);
        Assert.Equal(previsao.Skip(20).Take(20), segundaPagina.Itens);
        Assert.Equal(new[] { 1000m, 2000m, 1000m }, previsao.Take(3).Select(x => x.Valor));
        Assert.True(previsao[1].Ajustada);
        Assert.Empty((await Get<Pagina<Lancamento>>(client, "/api/lancamentos")).Itens);
        await Post<RegraRecorrente>(client, $"/api/recorrencias/{regra.Id}/versao", new DadosVersao(regra.Versao, mes.AddMonths(2), 1400));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/recorrencias/{regra.Id}/versao",
            new DadosVersao(regra.Versao, mes.AddMonths(2), 1600), Json)).StatusCode);

        DadosLancamento Dados(TipoLancamento tipo, decimal valor, DateOnly? data = null, Guid? lojaId = null) =>
            new(tipo, FormaPagamento.Pix, valor, data ?? mes.AddDays(5), conta.Id, tipo == TipoLancamento.RetiradaSaldo ? null : categoria,
                "Teste", Guid.NewGuid(), EstabelecimentoId: lojaId);
        var compra = Dados(TipoLancamento.Despesa, 1000) with { FormaPagamento = FormaPagamento.CartaoCredito, CartaoId = cartao.Id, Parcelas = 3 };
        var antes = await Post<EfeitoLancamento>(client, "/api/lancamentos/previsao", compra);
        Assert.Equal(0, antes.ImpactoNoSaldo);
        Assert.Equal(mes, antes.Faturas[0].Competencia);
        var apos = await Post<EfeitoLancamento>(client, "/api/lancamentos/previsao", compra with { DataLancamento = mes.AddDays(10) });
        Assert.Equal(mes.AddMonths(1), apos.Faturas[0].Competencia);
        await Post<ResultadoLancamento>(client, "/api/lancamentos", compra);
        var faturas = await Get<FaturaDetalhe[]>(client, $"/api/faturas?inicio={mes:yyyy-MM-dd}&meses=3");
        Assert.Equal(new[] { 333.33m, 333.33m, 333.34m }, faturas.Select(x => x.Total));
        Assert.Equal(Enumerable.Range(0, 3).Select(mes.AddMonths), faturas.Select(x => x.Competencia));
        var parcelas = await Get<CompraDetalhe[]>(client, $"/api/parcelas?mes={mes:yyyy-MM-dd}");
        Assert.Equal(1000m, Assert.Single(parcelas).FaltaPagar);
        var painel = $"/api/dashboard?inicio={mes:yyyy-MM-dd}&meses=1&visao=Competencia&horizonte=12";
        Assert.Equal(5000m, (await Get<Painel>(client, painel)).SaldoAtual);

        var pix = Dados(TipoLancamento.Despesa, 50);
        var salvos = await Task.WhenAll(Post<ResultadoLancamento>(client, "/api/lancamentos", pix), Post<ResultadoLancamento>(client, "/api/lancamentos", pix));
        Assert.Equal(salvos[0].Lancamentos, salvos[1].Lancamentos);
        Assert.Equal(4950m, (await Get<Painel>(client, painel)).SaldoAtual);
        Assert.Equal(faturas, await Get<FaturaDetalhe[]>(client, $"/api/faturas?inicio={mes:yyyy-MM-dd}&meses=3"));
        for (var i = 1; i <= 10; i++)
        {
            if (i == 6)
                await Ok(await client.PostAsJsonAsync($"/api/estabelecimentos/{variante.Id}/mesclar", new DadosMesclagem(loja.Id), Json));
            var resposta = await Post<ResultadoLancamento>(client, "/api/lancamentos",
                Dados(TipoLancamento.Despesa, 10, lojaId: i <= 5 ? variante.Id : loja.Id));
            Assert.Equal(i == 10, resposta.Alertas.Any(x => x.Contains("10 transações")));
        }
        var alertas = await Get<Pagina<Alerta>>(client, "/api/alertas?tamanho=100");
        var porLoja = Assert.Single(alertas.Itens, x => x.Tipo == TipoAlerta.MesmoEstabelecimento);
        Assert.Contains(alertas.Itens, x => x.Tipo == TipoAlerta.SaldoProjetado);
        await Ok(await client.PutAsJsonAsync($"/api/alertas/{porLoja.Id}/lido", new { }, Json));
        Assert.NotNull(Assert.Single((await Get<Pagina<Alerta>>(client, "/api/alertas?tamanho=100")).Itens, x => x.Id == porLoja.Id).LidoEm);
        await Post<ResultadoLancamento>(client, "/api/lancamentos", Dados(TipoLancamento.Receita, 7000));
        await Post<ResultadoLancamento>(client, "/api/lancamentos", Dados(TipoLancamento.AporteInvestimento, 1000));
        var antesResgate = await Get<Painel>(client, painel);
        await Post<ResultadoLancamento>(client, "/api/lancamentos", Dados(TipoLancamento.ResgateInvestimento, 500));
        var depoisResgate = await Get<Painel>(client, painel);
        Assert.Equal(antesResgate.SaldoAtual + 500, depoisResgate.SaldoAtual);
        Assert.Equal(antesResgate.PatrimonioInvestido - 500, depoisResgate.PatrimonioInvestido);
        Assert.Equal(antesResgate.Periodo[0].Entradas, depoisResgate.Periodo[0].Entradas);
        await Post<ResultadoLancamento>(client, "/api/lancamentos", Dados(TipoLancamento.RetiradaSaldo, 45));
        var antesPagamento = await Get<Painel>(client, painel);
        Assert.Equal(45m, antesPagamento.Periodo[0].Retiradas);
        Assert.Equal(500m, antesPagamento.Periodo[0].Resgates);
        Assert.Equal(1150m, antesPagamento.Periodo[0].Despesas);
        var pago = await Post<ResultadoLancamento>(client, $"/api/faturas/{faturas[0].Id}/pagar", new DadosPagamento(mes.AddDays(19), Guid.NewGuid()));
        var depoisPagamento = await Get<Painel>(client, painel);
        Assert.Equal(antesPagamento.SaldoAtual - 333.33m, depoisPagamento.SaldoAtual);
        Assert.Equal(antesPagamento.Periodo[0].Despesas, depoisPagamento.Periodo[0].Despesas);
        await Post<ResultadoLancamento>(client, $"/api/lancamentos/{pago.Lancamentos[0]}/estornar", new DadosEstorno(hoje, Guid.NewGuid(), null));
        Assert.Equal(antesPagamento.SaldoAtual, (await Get<Painel>(client, painel)).SaldoAtual);
        Assert.Empty((await Get<ResultadoConferencia>(client, "/api/conferencia")).Divergencias);

        await using (var escopo = factory.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<FinanceiroDb>();
            await using (var transacao = await db.Database.BeginTransactionAsync())
            {
                db.Add(new CompraParcelada { UsuarioId = conta.UsuarioId, CartaoId = cartao.Id, DataCompra = hoje, TotalParcelas = 3, ValorTotal = 1000 });
                await db.SaveChangesAsync();
                var invalida = await Assert.ThrowsAsync<PostgresException>(() => transacao.CommitAsync());
                Assert.Equal("23514", invalida.SqlState);
            }
            await db.Database.ExecuteSqlRawAsync("""
                CREATE OR REPLACE VIEW total_fatura AS
                SELECT f.usuario_id, f.id AS fatura_id,
                    COALESCE(SUM(CASE l.tipo WHEN 2 THEN l.valor WHEN 7 THEN -l.valor END), 0) + 0.01 AS total
                FROM fatura f
                LEFT JOIN lancamento l ON l.usuario_id = f.usuario_id AND l.fatura_id = f.id AND l.forma_pagamento = 6
                GROUP BY f.usuario_id, f.id
                """);
        }
        Assert.Equal(3, (await Get<ResultadoConferencia>(client, "/api/conferencia")).Divergencias.Length);

        using var outro = factory.CreateClient();
        await Entrar(outro, "outro@example.com");
        Assert.Empty((await Get<Pagina<Lancamento>>(outro, "/api/lancamentos")).Itens);
        Assert.Equal(HttpStatusCode.NotFound, (await outro.PostAsJsonAsync("/api/lancamentos", pix, Json)).StatusCode);
    }

    [Fact]
    public async Task ViradaDoMesConfirmaUmaVezEAceitaNovaVersaoNoInicio()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var mes = Mes(hoje).AddMonths(-1);
        using var client = factory!.CreateClient();
        await Entrar(client, "automatico@example.com");
        var categoria = (await Get<Cadastros>(client, "/api/cadastros")).Categorias[0].Id;
        var conta = await Post<Conta>(client, "/api/contas", new DadosConta("Reserva", 5000, mes));
        var regra = await Post<RegraRecorrente>(client, "/api/recorrencias",
            new DadosRegra(TipoRecorrencia.AporteFixo, 75, 1, mes, null, FormaPagamento.Pix, conta.Id, categoria, null, "Reserva mensal"));
        var sucessora = await Post<RegraRecorrente>(client, $"/api/recorrencias/{regra.Id}/versao", new DadosVersao(regra.Versao, mes, 90));
        await Ok(await client.PutAsJsonAsync("/api/recorrencias/confirmacao-automatica", new DadosConfirmacaoAutomatica(true), Json));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceiroDb>();
            await db.Set<ConfiguracaoUsuario>().Where(x => x.UsuarioId == conta.UsuarioId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.ConfirmacaoAutomaticaDesde, mes));
        }
        var confirmar = factory.Services.GetRequiredService<ConfirmacoesAutomaticas>();
        await confirmar.ProcessarAsync(hoje, CancellationToken.None);
        await confirmar.ProcessarAsync(hoje, CancellationToken.None);
        var lancamento = Assert.Single((await Get<Pagina<Lancamento>>(client, "/api/lancamentos")).Itens);
        Assert.Equal(90m, lancamento.Valor);
        Assert.Equal(sucessora.Id, lancamento.RegraRecorrenteId);
        Assert.Equal(mes, lancamento.Competencia);
        Assert.Equal(Mes(hoje), (await Get<ConfiguracaoUsuario>(client, "/api/alertas/configuracao")).ConfirmacaoAutomaticaDesde);
        await Post<RegraRecorrente>(client, $"/api/recorrencias/{sucessora.Id}/encerrar", new DadosEncerramento(sucessora.Versao, Mes(hoje)));
        var restantes = await Get<OcorrenciaPrevista[]>(client, $"/api/recorrencias/previsao?inicio={mes:yyyy-MM-dd}&meses=3");
        Assert.True(Assert.Single(restantes).Realizada);
    }

    [Fact]
    public void ParcelasPositivasSemPerderCentavos()
    {
        Assert.Equal(new[] { 333.33m, 333.33m, 333.34m }, Parcelar(1000, 3));
        for (var centavos = 1; centavos <= 500; centavos++)
            for (var quantidade = 1; quantidade <= Math.Min(120, centavos); quantidade++)
            {
                var parcelas = Parcelar(centavos / 100m, quantidade);
                Assert.All(parcelas, x => Assert.True(x > 0 && decimal.Round(x, 2) == x));
                Assert.Equal(centavos / 100m, parcelas.Sum());
            }
    }

    private static async Task Entrar(HttpClient client, string email)
    {
        var credenciais = new Credenciais(email, "Teste!012345");
        await Token(client);
        await Ok(await client.PostAsJsonAsync("/api/sessao/registrar",
            new CadastroUsuario(email, credenciais.Senha, email, credenciais.Senha), Json));
        await Ok(await client.PostAsJsonAsync("/api/sessao/entrar", credenciais, Json));
        await Token(client);
    }

    private static async Task Token(HttpClient client)
    {
        var resposta = await client.GetAsync("/api/sessao/token");
        await Ok(resposta);
        var token = resposta.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("XSRF-TOKEN=")).Split(';')[0]["XSRF-TOKEN=".Length..];
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", Uri.UnescapeDataString(token));
    }

    private static async Task<T> Post<T>(HttpClient client, string uri, object dados)
    {
        var resposta = await client.PostAsJsonAsync(uri, dados, Json);
        await Ok(resposta);
        return (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private static async Task<T> Get<T>(HttpClient client, string uri)
    {
        var resposta = await client.GetAsync(uri);
        await Ok(resposta);
        return (await resposta.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private static async Task Ok(HttpResponseMessage response) =>
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri} {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

    private static async Task ConferirProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status)
    {
        Assert.Equal(status, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);

        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problema.GetProperty("status").GetInt32());
    }
}
