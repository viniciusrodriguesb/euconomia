using Api.Contratos;
using Aplicacao;
using Dominio;
using Infraestrutura;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[Route("api/alertas")]
public sealed class AlertasController(ISender mediador, UnidadeFinanceira unidade, FinanceiroDb banco) : ApiControllerBase
{
    [HttpGet]
    public Task<Pagina<Alerta>> Listar(CancellationToken cancellationToken, int pagina = 1, int tamanho = 20) =>
        unidade.Ler<Alerta>(UsuarioId)
            .OrderBy(alerta => alerta.LidoEm.HasValue)
            .ThenByDescending(alerta => alerta.Competencia)
            .ThenByDescending(alerta => alerta.Id)
            .PaginarAsync(pagina, tamanho, cancellationToken);

    [HttpPut("{id:guid}/lido")]
    public async Task<IActionResult> MarcarComoLido(Guid id, CancellationToken cancellationToken)
    {
        await mediador.Send(new LerAlerta(UsuarioId, id), cancellationToken);
        return NoContent();
    }

    [HttpGet("configuracao")]
    public Task<ConfiguracaoUsuario> ObterConfiguracao(CancellationToken cancellationToken)
    {
        var usuarioId = UsuarioId;

        return banco.Set<ConfiguracaoUsuario>().AsNoTracking()
            .SingleAsync(configuracao => configuracao.UsuarioId == usuarioId, cancellationToken);
    }

    [HttpPut("configuracao")]
    public Task<ConfiguracaoUsuario> AtualizarConfiguracao(DadosAlertas dados, CancellationToken cancellationToken) =>
        mediador.Send(new ConfigurarAlertas(UsuarioId, dados.Amarelo, dados.Vermelho, dados.Fixos,
            dados.Comprometimento, dados.Cartao, dados.Estabelecimento, dados.Atipica, dados.AlertasDesativados), cancellationToken);
}
