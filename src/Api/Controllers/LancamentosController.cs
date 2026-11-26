using Api.Contratos;
using Aplicacao;
using Dominio;
using Infraestrutura;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/lancamentos")]
public sealed class LancamentosController(ISender mediador, UnidadeFinanceira unidade) : ApiControllerBase
{
    [HttpGet]
    public Task<Pagina<Lancamento>> Listar(CancellationToken cancellationToken, int pagina = 1, int tamanho = 20, DateOnly? competencia = null) =>
        mediador.Send(new ListarLancamentos(UsuarioId, pagina, tamanho, competencia), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<Lancamento> Obter(Guid id, CancellationToken cancellationToken) =>
        unidade.ObterAsync<Lancamento>(UsuarioId, id, "Lançamento não encontrado. Atualize o extrato.", cancellationToken);

    [HttpPost("previsao")]
    public Task<EfeitoLancamento> Prever(DadosLancamento dados, CancellationToken cancellationToken) =>
        mediador.Send(new PreverLancamento(UsuarioId, dados), cancellationToken);

    [HttpPost]
    public Task<ResultadoLancamento> Registrar(DadosLancamento dados, CancellationToken cancellationToken) =>
        mediador.Send(new RegistrarLancamento(UsuarioId, dados), cancellationToken);

    [HttpPost("{id:guid}/estornar")]
    public Task<ResultadoLancamento> Estornar(Guid id, DadosEstorno dados, CancellationToken cancellationToken) =>
        mediador.Send(new EstornarLancamento(UsuarioId, id, dados.Data, dados.ChaveIdempotencia, dados.Substituto), cancellationToken);
}
