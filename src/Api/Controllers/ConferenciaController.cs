using Aplicacao;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/conferencia")]
public sealed class ConferenciaController(ISender mediador) : ApiControllerBase
{
    [HttpGet]
    public Task<ResultadoConferencia> Conferir(CancellationToken cancellationToken) =>
        mediador.Send(new ConferirFinanceiro(UsuarioId), cancellationToken);
}
