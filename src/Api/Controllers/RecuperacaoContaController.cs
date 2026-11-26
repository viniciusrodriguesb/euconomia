using Api.Contratos;
using Api.Validacoes;
using Infraestrutura;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[Route("api/recuperacao-conta")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RecuperacaoContaController(RecuperacaoConta recuperacao) : ApiControllerBase
{
    [HttpPost("codigo")]
    [EnableRateLimiting("recuperacao")]
    public async Task<IActionResult> Gerar(GerarCodigoRecuperacao pedido, CancellationToken ct)
    {
        CredenciaisValidador.ValidarEntrada(new("autenticado", pedido.Senha));
        var codigo = await recuperacao.GerarAsync(UsuarioId, pedido.Senha, ct);
        return codigo is null
            ? Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "Não foi possível gerar o código. Confira sua senha e tente novamente.")
            : Ok(new CodigoRecuperacao(codigo));
    }

    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [EnableRateLimiting("recuperacao")]
    public async Task<IActionResult> Redefinir(RedefinirSenha pedido, CancellationToken ct)
    {
        RedefinirSenhaValidador.Validar(pedido);
        return await recuperacao.RedefinirAsync(pedido.Email, pedido.Codigo, pedido.Senha, ct)
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "Não foi possível redefinir a senha. Confira os dados e o código de recuperação.");
    }
}
