using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/cartoes")]
public sealed class CartoesController(ISender mediador) : ApiControllerBase
{
    [HttpPost]
    public Task<Cartao> Registrar(DadosCartao dados, CancellationToken cancellationToken) =>
        mediador.Send(new SalvarCartao(UsuarioId, null, dados), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<Cartao> Atualizar(Guid id, DadosCartao dados, CancellationToken cancellationToken) =>
        mediador.Send(new SalvarCartao(UsuarioId, id, dados), cancellationToken);
}
