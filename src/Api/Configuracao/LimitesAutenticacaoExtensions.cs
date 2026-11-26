using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Configuracao;

internal static class LimitesAutenticacaoExtensions
{
    public static IServiceCollection AdicionarLimitesAutenticacao(this IServiceCollection servicos)
    {
        servicos.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.OnRejected = async (contexto, ct) =>
            {
                contexto.HttpContext.Response.Headers.RetryAfter = "900";
                await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests,
                    detail: "Muitas tentativas. Aguarde 15 minutos antes de tentar novamente.")
                    .ExecuteAsync(contexto.HttpContext);
            };
            opcoes.AddPolicy("autenticacao", contexto => CriarLimite(contexto, 30));
            opcoes.AddPolicy("recuperacao", contexto => CriarLimite(contexto, 10));
        });
        return servicos;
    }

    private static RateLimitPartition<string> CriarLimite(HttpContext contexto, int limite) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limite,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            });
}
