using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/contas")]
public sealed class ContasController(ISender mediador) : ApiControllerBase
{
    [HttpPost]
    public Task<Conta> Registrar(DadosConta dados, CancellationToken cancellationToken) =>
        mediador.Send(new RegistrarConta(UsuarioId, dados), cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        await mediador.Send(new InativarCadastro(UsuarioId, id, Conta: true), cancellationToken);
        return NoContent();
    }
}
