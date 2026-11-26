using System.Text.Json.Serialization;
using Api.Middlewares;
using Api.Serializacao;
using Api.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Configuracao;

internal static class InterfaceHttpExtensions
{
    public static IServiceCollection AdicionarInterfaceHttp(this IServiceCollection servicos)
    {
        servicos.AddHealthChecks().AddCheck<SaudeBanco>("postgresql");
        servicos.AddControllers(opcoes => opcoes.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .ConfigureApiBehaviorOptions(opcoes =>
            {
                opcoes.InvalidModelStateResponseFactory = _ => new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Detail = RespostasDeErro.DadosInvalidos
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ContentTypes = { "application/problem+json" }
                };
            })
            .AddJsonOptions(opcoes =>
            {
                opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
                opcoes.JsonSerializerOptions.Converters.Add(new DecimalJsonConverter());
            });

        return servicos;
    }

    public static WebApplication ConfigurarPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UsarTratamentoDeErros();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseWhen(contexto => !contexto.Request.Path.StartsWithSegments("/health"),
                pipeline => pipeline.UseHttpsRedirection());
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<AntifalsificacaoMiddleware>();

        return app;
    }

    public static void MapearRotas(this WebApplication app)
    {
        app.MapHealthChecks("/health/ready").AllowAnonymous();
        app.MapControllers();
        app.MapFallback("/api/{**caminho}", () => Results.NotFound());
        app.MapFallbackToFile("index.html");
    }
}
