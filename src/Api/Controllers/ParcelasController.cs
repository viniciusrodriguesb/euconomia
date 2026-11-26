using Aplicacao;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/parcelas")]
public sealed class ParcelasController(ISender mediador) : ApiControllerBase
{
    [HttpGet]
    public Task<CompraDetalhe[]> Listar([BindRequired] DateOnly mes, CancellationToken cancellationToken) =>
        mediador.Send(new ListarParcelas(UsuarioId, mes), cancellationToken);
}
