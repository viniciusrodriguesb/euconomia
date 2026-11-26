using Microsoft.AspNetCore.Diagnostics;

namespace Api.Middlewares;

internal static class RespostasDeErro
{
    public const string DadosInvalidos = "Confira os campos enviados e tente novamente.";

    public static Task EscreverStatusAsync(StatusCodeContext contexto)
    {
        var status = contexto.HttpContext.Response.StatusCode;
        var mensagem = status switch
        {
            StatusCodes.Status401Unauthorized => "Entre na sua conta para continuar.",
            StatusCodes.Status403Forbidden => "Sua conta não tem acesso a esta operação.",
            StatusCodes.Status404NotFound => "Endereço não encontrado. Volte à página inicial.",
            _ => DadosInvalidos
        };

        return Results.Problem(detail: mensagem, statusCode: status).ExecuteAsync(contexto.HttpContext);
    }
}
