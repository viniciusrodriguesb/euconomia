using Aplicacao;
using Dominio;
using MediatR;

namespace Infraestrutura;


public sealed class EstornarLancamentoHandler(FinanceiroDb banco, UnidadeFinanceira unidade, RegistroLancamentos registro, AlertasFinanceiros alertas)
    : IRequestHandler<EstornarLancamento, ResultadoLancamento>
{
    public Task<ResultadoLancamento> Handle(EstornarLancamento requisicao, CancellationToken cancellationToken) =>
        unidade.ExecutarIdempotenteAsync(requisicao.UsuarioId, requisicao.ChaveIdempotencia, requisicao, async () =>
        {
            var original = await unidade.ObterAsync<Lancamento>(requisicao.UsuarioId, requisicao.LancamentoId,
                "Lançamento não encontrado. Atualize o extrato.", cancellationToken);
            await EstornoValidador.ValidarOriginalAsync(unidade, requisicao, original, cancellationToken);
            await AjustarFaturaAsync(requisicao, original, cancellationToken);

            var estorno = new Lancamento
            {
                UsuarioId = requisicao.UsuarioId,
                Tipo = TipoLancamento.Estorno,
                FormaPagamento = original.FormaPagamento,
                Valor = original.Valor,
                Moeda = original.Moeda,
                DataLancamento = requisicao.Data,
                Competencia = original.Competencia,
                ContaId = original.ContaId,
                CartaoId = original.CartaoId,
                FaturaId = original.FaturaId,
                CategoriaId = original.CategoriaId,
                EstabelecimentoId = original.EstabelecimentoId,
                Descricao = "Estorno de " + original.Descricao[..Math.Min(220, original.Descricao.Length)],
                LancamentoEstornadoId = original.Id,
                ChaveIdempotencia = requisicao.ChaveIdempotencia
            };
            banco.Add(estorno);
            unidade.Auditar(requisicao.UsuarioId, estorno.Id, "Estornar", original, estorno);
            var criados = new List<Lancamento> { estorno };
            if (requisicao.Substituto is { } substituto)
            {
                EstornoValidador.ValidarSubstituto(requisicao, substituto);
                criados.AddRange(await registro.CriarAsync(requisicao.UsuarioId, substituto, null, cancellationToken));
            }
            return new(criados.Select(x => x.Id).ToArray(), await alertas.AvaliarGravacaoAsync(requisicao.UsuarioId, criados.ToArray(), cancellationToken));
        }, cancellationToken);

    private async Task AjustarFaturaAsync(EstornarLancamento requisicao, Lancamento original, CancellationToken cancellationToken)
    {
        if (original.FaturaId is { } id)
        {
            var fatura = await unidade.ObterAsync<Fatura>(requisicao.UsuarioId, id, "Fatura não encontrada.", cancellationToken);
            if (original.Tipo == TipoLancamento.PagamentoFatura)
            {
                if (fatura.LancamentoPagamentoId != original.Id)
                    throw new BusinessException("O lançamento não é o pagamento ativo da fatura.");
                banco.Attach(fatura);
                fatura.Status = StatusFatura.Fechada;
                fatura.LancamentoPagamentoId = null;
            }
            else if (fatura.Status == StatusFatura.Paga)
                throw new BusinessException("Estorne o pagamento da fatura antes de corrigir esta compra.");
        }
    }
}
