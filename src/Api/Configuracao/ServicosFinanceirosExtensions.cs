using Api.Servicos;
using Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace Api.Configuracao;

internal static class ServicosFinanceirosExtensions
{
    public static IServiceCollection AdicionarPersistenciaFinanceira(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var conexao = configuracao.GetConnectionString("Financeiro")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Financeiro no appsettings.Development.json ou defina a variável ConnectionStrings__Financeiro antes de iniciar a API.");

        servicos.AddDbContext<FinanceiroDb>(opcoes => opcoes.UseNpgsql(conexao));
        servicos.AddScoped<UnidadeFinanceira>();

        return servicos;
    }

    public static IServiceCollection AdicionarCasosDeUso(this IServiceCollection servicos)
    {
        servicos.AddMediatR(opcoes => opcoes.RegisterServicesFromAssemblyContaining<LancamentosHandler>());
        servicos.AddScoped<RegistroLancamentos>();
        servicos.AddScoped<AlertasFinanceiros>();
        servicos.AddScoped<ProvisionarUsuario>();
        servicos.AddScoped<RecuperacaoConta>();

        servicos.AddSingleton<ConfirmacoesAutomaticas>();
        servicos.AddHostedService(servicos => servicos.GetRequiredService<ConfirmacoesAutomaticas>());

        return servicos;
    }
}
