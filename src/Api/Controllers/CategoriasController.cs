using Api.Contratos;
using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/categorias")]
public sealed class CategoriasController(ISender mediador) : ApiControllerBase
{
    [HttpPost]
    public Task<Categoria> Registrar(NomeCadastro dados, CancellationToken cancellationToken) =>
        mediador.Send(new RegistrarCategoria(UsuarioId, dados.Nome), cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        await mediador.Send(new InativarCadastro(UsuarioId, id, Conta: false), cancellationToken);
        return NoContent();
    }
}
