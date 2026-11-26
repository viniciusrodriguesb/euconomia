using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class ContasCadastroHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<RegistrarConta, Conta>
{
    public async Task<Conta> Handle(RegistrarConta requisicao, CancellationToken cancellationToken)
    {
        var dados = requisicao.Dados;
        ContaValidador.Validar(dados);

        var conta = new Conta { UsuarioId = requisicao.UsuarioId, Nome = NomeCadastroValidador.Validar(dados.Nome), SaldoInicial = dados.SaldoInicial, DataSaldoInicial = dados.DataSaldoInicial };
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        if (await unidade.Ler<Conta>(requisicao.UsuarioId).AnyAsync(x => x.Nome == conta.Nome && x.InativadoEm == null, cancellationToken))
            throw new BusinessException("Já existe uma conta ativa com este nome.");

        banco.Add(conta);

        await unidade.CommitAsync(transacao, cancellationToken);

        return conta;
    }
}
