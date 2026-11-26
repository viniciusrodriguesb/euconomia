using Api.Contratos;
using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/estabelecimentos")]
public sealed class EstabelecimentosController(ISender mediador) : ApiControllerBase
{
    [HttpPost]
    public Task<Estabelecimento> Registrar(DadosEstabelecimento dados, CancellationToken cancellationToken) =>
        mediador.Send(new RegistrarEstabelecimento(UsuarioId, dados), cancellationToken);

    [HttpPost("{id:guid}/aliases")]
    public Task<AliasEstabelecimento> AssociarNome(Guid id, NomeCadastro dados, CancellationToken cancellationToken) =>
        mediador.Send(new AssociarAlias(UsuarioId, id, dados.Nome), cancellationToken);

    [HttpPost("{id:guid}/mesclar")]
    public async Task<IActionResult> Mesclar(Guid id, DadosMesclagem dados, CancellationToken cancellationToken)
    {
        await mediador.Send(new MesclarEstabelecimento(UsuarioId, id, dados.DestinoId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/limite")]
    public Task<Estabelecimento> AtualizarLimite(Guid id, DadosLimiteLoja dados, CancellationToken cancellationToken) =>
        mediador.Send(new AjustarLimiteEstabelecimento(UsuarioId, id, dados.LimiteTransacoes), cancellationToken);
}
