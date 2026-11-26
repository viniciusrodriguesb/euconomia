using Api.Contratos;
using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/recorrencias")]
public sealed class RecorrenciasController(ISender mediador) : ApiControllerBase
{
    [HttpPost]
    public Task<RegraRecorrente> Registrar(DadosRegra dados, CancellationToken cancellationToken) =>
        mediador.Send(new RegistrarRegra(UsuarioId, dados), cancellationToken);

    [HttpGet("previsao")]
    public Task<OcorrenciaPrevista[]> ListarPrevisoes([BindRequired] DateOnly inicio, [BindRequired] int meses, CancellationToken cancellationToken) =>
        mediador.Send(new ListarPrevisao(UsuarioId, inicio, meses), cancellationToken);

    [HttpGet("previsao/paginada")]
    public Task<Pagina<OcorrenciaPrevista>> ListarPrevisoesPaginadas([BindRequired] DateOnly inicio, [BindRequired] int meses,
        CancellationToken cancellationToken, int pagina = 1, int tamanho = 10) =>
        mediador.Send(new ListarPrevisaoPaginada(UsuarioId, inicio, meses, pagina, tamanho), cancellationToken);

    [HttpPost("{id:guid}/versao")]
    public Task<RegraRecorrente> Versionar(Guid id, DadosVersao dados, CancellationToken cancellationToken) =>
        mediador.Send(new VersionarRegra(UsuarioId, id, dados.Versao, dados.Competencia, dados.Valor), cancellationToken);

    [HttpPost("{id:guid}/encerrar")]
    public Task<RegraRecorrente> Encerrar(Guid id, DadosEncerramento dados, CancellationToken cancellationToken) =>
        mediador.Send(new EncerrarRegra(UsuarioId, id, dados.Versao, dados.Competencia), cancellationToken);

    [HttpPut("{id:guid}/excecao")]
    public async Task<IActionResult> Ajustar(Guid id, DadosExcecao dados, CancellationToken cancellationToken)
    {
        var comando = new AjustarRecorrencia(UsuarioId, id, dados.VersaoRegra, dados.Competencia,
            dados.VersaoExcecao, dados.Valor, dados.Ignorada, dados.Restaurar);

        return new JsonResult(await mediador.Send(comando, cancellationToken));
    }

    [HttpPost("{id:guid}/confirmar")]
    public Task<ResultadoLancamento> Confirmar(Guid id, DadosConfirmacao dados, CancellationToken cancellationToken) =>
        mediador.Send(new ConfirmarRecorrencia(UsuarioId, id, dados.Competencia, dados.ChaveIdempotencia,
            EfeitoEsperado: dados.EfeitoEsperado), cancellationToken);

    [HttpPut("confirmacao-automatica")]
    public Task<ConfiguracaoUsuario> ConfigurarConfirmacaoAutomatica(DadosConfirmacaoAutomatica dados, CancellationToken cancellationToken) =>
        mediador.Send(new ConfigurarConfirmacao(UsuarioId, dados.Ativa), cancellationToken);
}
