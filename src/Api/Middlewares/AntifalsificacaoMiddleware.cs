using Microsoft.AspNetCore.Antiforgery;

namespace Api.Middlewares;

internal sealed class AntifalsificacaoMiddleware(RequestDelegate proximo)
{
    public async Task InvokeAsync(HttpContext contexto, IAntiforgery antifalsificacao)
    {
        if (contexto.Request.Path.StartsWithSegments("/api") && ExigeToken(contexto.Request.Method))
            await antifalsificacao.ValidateRequestAsync(contexto);

        await proximo(contexto);
    }

    private static bool ExigeToken(string metodo) =>
        HttpMethods.IsPost(metodo) || HttpMethods.IsPut(metodo) || HttpMethods.IsPatch(metodo) || HttpMethods.IsDelete(metodo);
}
