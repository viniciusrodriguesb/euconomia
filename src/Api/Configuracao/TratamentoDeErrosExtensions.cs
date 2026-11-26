using Api.Middlewares;

namespace Api.Configuracao;

internal static class TratamentoDeErrosExtensions
{
    public static IServiceCollection AdicionarTratamentoDeErros(this IServiceCollection servicos)
    {
        servicos.AddProblemDetails();
        servicos.AddExceptionHandler<TratadorDeExcecoes>();

        return servicos;
    }

    public static void UsarTratamentoDeErros(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages(RespostasDeErro.EscreverStatusAsync);
    }
}
