using Aplicacao;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Middlewares;

internal sealed class TratadorDeExcecoes(ILogger<TratadorDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken cancellationToken)
    {
        var (status, mensagem) = excecao switch
        {
            NotFoundException erro => (StatusCodes.Status404NotFound, erro.Message),
            ConcorrenciaException erro => (StatusCodes.Status409Conflict, erro.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, new ConcorrenciaException().Message),
            BusinessException erro => (StatusCodes.Status422UnprocessableEntity, erro.Message),
            AntiforgeryValidationException => (StatusCodes.Status400BadRequest, "Atualize a página para renovar a sessão e tente novamente."),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, RespostasDeErro.DadosInvalidos),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Já existe um registro com estes dados. Atualize a lista e confira o lançamento."),
            _ => (StatusCodes.Status500InternalServerError, "Não foi possível concluir a operação. Tente novamente em instantes.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(excecao, "Erro inesperado durante o processamento da requisição.");

        await Results.Problem(detail: mensagem, statusCode: status).ExecuteAsync(contexto);
        return true;
    }
}
