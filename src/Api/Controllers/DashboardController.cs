using Aplicacao;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/dashboard")]
public sealed class DashboardController(ISender mediador) : ApiControllerBase
{
    [HttpGet]
    public Task<Painel> Obter([BindRequired] DateOnly inicio, [BindRequired] int meses,
        [BindRequired] VisaoRelatorio visao, [BindRequired] int horizonte, CancellationToken cancellationToken) =>
        mediador.Send(new ObterDashboard(UsuarioId, inicio, meses, visao, horizonte), cancellationToken);
}
