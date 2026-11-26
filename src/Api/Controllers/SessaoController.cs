using Api.Contratos;
using Api.Validacoes;
using Infraestrutura;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[Route("api/sessao")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SessaoController(SignInManager<Usuario> autenticacao, ProvisionarUsuario provisionamento, IAntiforgery antifalsificacao, IWebHostEnvironment ambiente) : ApiControllerBase
{
    [HttpGet("token")]
    [AllowAnonymous]
    public IActionResult ObterToken()
    {
        var tokens = antifalsificacao.GetAndStoreTokens(HttpContext);
        var opcoes = new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps || !ambiente.IsDevelopment(),
            Path = "/"
        };

        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, opcoes);
        return NoContent();
    }

    [HttpGet]
    public IActionResult ObterSessao() => Ok(new { Email = User.Identity!.Name });

    [HttpPost("registrar")]
    [AllowAnonymous]
    [EnableRateLimiting("autenticacao")]
    public async Task<IActionResult> Registrar(CadastroUsuario credenciais, CancellationToken cancellationToken)
    {
        CadastroUsuarioValidador.Validar(credenciais);

        var resultado = await provisionamento.RegistrarAsync(credenciais.Email, credenciais.Senha, cancellationToken);
        return resultado.Succeeded
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "Confira se o e-mail já está cadastrado e se a senha contém maiúscula, minúscula, número e símbolo.");
    }

    [HttpPost("entrar")]
    [AllowAnonymous]
    [EnableRateLimiting("autenticacao")]
    public async Task<IActionResult> Entrar(Credenciais credenciais)
    {
        CredenciaisValidador.ValidarEntrada(credenciais);

        var resultado = await autenticacao.PasswordSignInAsync(credenciais.Email, credenciais.Senha, isPersistent: false, lockoutOnFailure: true);
        return resultado.Succeeded
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status401Unauthorized,
                detail: "Confira o e-mail e a senha ou aguarde alguns minutos após várias tentativas.");
    }

    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    {
        await autenticacao.SignOutAsync();
        return NoContent();
    }
}
