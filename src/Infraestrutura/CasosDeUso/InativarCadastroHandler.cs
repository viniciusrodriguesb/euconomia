using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class InativarCadastroHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<InativarCadastro>
{
    public async Task Handle(InativarCadastro requisicao, CancellationToken cancellationToken)
    {
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        if (requisicao.Conta)
        {
            var conta = await unidade.ObterAsync<Conta>(requisicao.UsuarioId, requisicao.Id, "Conta não encontrada.", cancellationToken);
            if (await unidade.Ler<Cartao>(requisicao.UsuarioId).AnyAsync(x => x.ContaPagamentoId == conta.Id && x.InativadoEm == null, cancellationToken))
                throw new BusinessException("Altere a conta de pagamento dos cartões ativos antes de inativar esta conta.");
            if (await PrevisaoRecorrencias.Vigentes(unidade, requisicao.UsuarioId).AnyAsync(x => x.ContaId == conta.Id, cancellationToken))
                throw new BusinessException("Encerre as recorrências vigentes desta conta antes de inativá-la.");
            banco.Attach(conta);
            conta.InativadoEm ??= DateTime.Now;
        }
        else
        {
            var categoria = await unidade.ObterAsync<Categoria>(requisicao.UsuarioId, requisicao.Id, "Categoria não encontrada.", cancellationToken);
            if (await PrevisaoRecorrencias.Vigentes(unidade, requisicao.UsuarioId).AnyAsync(x => x.CategoriaId == categoria.Id, cancellationToken))
                throw new BusinessException("Encerre as recorrências vigentes desta categoria antes de inativá-la.");
            banco.Attach(categoria);
            categoria.InativadoEm ??= DateTime.Now;
        }

        await unidade.CommitAsync(transacao, cancellationToken);
    }
}
