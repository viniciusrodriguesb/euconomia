using Api.Contratos;
using Aplicacao;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/faturas")]
public sealed class FaturasController(ISender mediador) : ApiControllerBase
{
    [HttpGet]
    public Task<FaturaDetalhe[]> Listar([BindRequired] DateOnly inicio, [BindRequired] int meses, CancellationToken cancellationToken) =>
        mediador.Send(new ListarFaturas(UsuarioId, inicio, meses), cancellationToken);

    [HttpPost("{id:guid}/pagar")]
    public Task<ResultadoLancamento> Pagar(Guid id, DadosPagamento dados, CancellationToken cancellationToken) =>
        mediador.Send(new PagarFatura(UsuarioId, id, dados.Data, dados.ChaveIdempotencia, dados.ValorEsperado), cancellationToken);
}
