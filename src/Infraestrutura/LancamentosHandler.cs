using Aplicacao;
using MediatR;

namespace Infraestrutura;


public sealed class LancamentosHandler(UnidadeFinanceira unidade, RegistroLancamentos registro, AlertasFinanceiros alertas)
    : IRequestHandler<PreverLancamento, EfeitoLancamento>,
      IRequestHandler<RegistrarLancamento, ResultadoLancamento>
{
    public Task<EfeitoLancamento> Handle(PreverLancamento requisicao, CancellationToken cancellationToken) =>
        registro.PreverAsync(requisicao.UsuarioId, requisicao.Dados, cancellationToken);

    public Task<ResultadoLancamento> Handle(RegistrarLancamento requisicao, CancellationToken cancellationToken) =>
        unidade.ExecutarIdempotenteAsync(requisicao.UsuarioId, requisicao.Dados.ChaveIdempotencia, requisicao,
            async () =>
            {
                var criados = await registro.CriarAsync(requisicao.UsuarioId, requisicao.Dados, null, cancellationToken);
                return new(criados.Select(x => x.Id).ToArray(), await alertas.AvaliarGravacaoAsync(requisicao.UsuarioId, criados, cancellationToken));
            }, cancellationToken);
}
