using Aplicacao;
using Dominio;
using MediatR;

namespace Infraestrutura;

public sealed class LerAlertaHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<LerAlerta>
{
    public async Task Handle(LerAlerta requisicao, CancellationToken cancellationToken)
    {
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var alerta = await unidade.ObterAsync<Alerta>(requisicao.UsuarioId, requisicao.Id, "Alerta não encontrado.", cancellationToken);
        banco.Attach(alerta);
        alerta.LidoEm ??= DateTime.Now;

        await unidade.CommitAsync(transacao, cancellationToken);
    }
}
