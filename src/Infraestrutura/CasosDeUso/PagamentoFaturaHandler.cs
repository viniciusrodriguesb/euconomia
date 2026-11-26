using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;


public sealed class PagamentoFaturaHandler(FinanceiroDb banco, UnidadeFinanceira unidade, AlertasFinanceiros alertas)
    : IRequestHandler<PagarFatura, ResultadoLancamento>
{
    public Task<ResultadoLancamento> Handle(PagarFatura requisicao, CancellationToken cancellationToken) =>
        unidade.ExecutarIdempotenteAsync(requisicao.UsuarioId, requisicao.ChaveIdempotencia, requisicao, async () =>
        {
            var fatura = await unidade.ObterAsync<Fatura>(requisicao.UsuarioId, requisicao.FaturaId, "Fatura não encontrada. Confira o cartão.", cancellationToken);
            var cartao = await unidade.ObterAsync<Cartao>(requisicao.UsuarioId, fatura.CartaoId, "Cartão não encontrado.", cancellationToken);
            var conta = await unidade.ObterAsync<Conta>(requisicao.UsuarioId, cartao.ContaPagamentoId, "Conta de pagamento não encontrada.", cancellationToken);
            PagamentoFaturaValidador.ValidarData(requisicao, fatura, conta);

            var total = await TotalFaturaAsync(requisicao.UsuarioId, fatura.Id, cancellationToken);
            PagamentoFaturaValidador.ValidarTotal(total, requisicao.ValorEsperado);

            var pagamento = new Lancamento
            {
                UsuarioId = requisicao.UsuarioId,
                Tipo = TipoLancamento.PagamentoFatura,
                FormaPagamento = FormaPagamento.Transferencia,
                Valor = total,
                DataLancamento = requisicao.Data,
                Competencia = fatura.Competencia,
                ContaId = conta.Id,
                CartaoId = cartao.Id,
                FaturaId = fatura.Id,
                Descricao = $"Pagamento da fatura {cartao.Nome}",
                ChaveIdempotencia = requisicao.ChaveIdempotencia
            };
            banco.Attach(fatura);
            banco.Add(pagamento);
            fatura.Status = StatusFatura.Paga;
            fatura.LancamentoPagamentoId = pagamento.Id;
            return new([pagamento.Id], await alertas.AvaliarGravacaoAsync(requisicao.UsuarioId, [pagamento], cancellationToken));
        }, cancellationToken);

    private Task<decimal> TotalFaturaAsync(Guid usuarioId, Guid fatura, CancellationToken cancellationToken) =>
        unidade.Ler<Lancamento>(usuarioId).Where(x => x.FaturaId == fatura && x.FormaPagamento == FormaPagamento.CartaoCredito)
            .SumAsync(x => x.Tipo == TipoLancamento.Estorno ? -x.Valor : x.Valor, cancellationToken);
}
