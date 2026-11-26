using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class CartoesCadastroHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<SalvarCartao, Cartao>
{
    public async Task<Cartao> Handle(SalvarCartao requisicao, CancellationToken cancellationToken)
    {
        var dados = requisicao.Dados;
        CartaoValidador.Validar(dados);

        var nome = NomeCadastroValidador.Validar(dados.Nome);
        var bandeira = NomeCadastroValidador.Validar(dados.Bandeira);
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var conta = await unidade.ObterAsync<Conta>(requisicao.UsuarioId, dados.ContaPagamentoId, "Conta de pagamento não encontrada.", cancellationToken);
        if (conta.InativadoEm.HasValue && !dados.Inativo)
            throw new BusinessException("Selecione uma conta de pagamento ativa.");
        Cartao cartao;
        Cartao? antes = null;
        if (requisicao.Id is { } id)
        {
            cartao = await unidade.ObterAsync<Cartao>(requisicao.UsuarioId, id, "Cartão não encontrado.", cancellationToken);
            if (dados.Versao != cartao.Versao)
                throw new ConcorrenciaException();
            if (dados.Inativo && !cartao.InativadoEm.HasValue &&
                await PrevisaoRecorrencias.Vigentes(unidade, requisicao.UsuarioId).AnyAsync(x => x.CartaoId == cartao.Id, cancellationToken))
                throw new BusinessException("Encerre as recorrências vigentes deste cartão antes de inativá-lo.");
            antes = (Cartao)banco.Entry(cartao).CurrentValues.ToObject();
            banco.Attach(cartao);
        }
        else
        {
            cartao = new() { UsuarioId = requisicao.UsuarioId, Nome = nome, Bandeira = bandeira };
            banco.Add(cartao);
        }
        cartao.Nome = nome;
        cartao.Bandeira = bandeira;
        cartao.Limite = dados.Limite;
        cartao.DiaFechamento = dados.DiaFechamento;
        cartao.DiaVencimento = dados.DiaVencimento;
        cartao.ContaPagamentoId = dados.ContaPagamentoId;
        cartao.InativadoEm = dados.Inativo ? cartao.InativadoEm ?? DateTime.Now : null;
        unidade.Auditar(requisicao.UsuarioId, cartao.Id, antes is null ? "Criar" : "Atualizar", antes, cartao);

        await unidade.CommitAsync(transacao, cancellationToken);

        return cartao;
    }
}
