using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

public sealed class PreferenciasHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<ConfigurarAlertas, ConfiguracaoUsuario>,
      IRequestHandler<ConfigurarConfirmacao, ConfiguracaoUsuario>
{
    public async Task<ConfiguracaoUsuario> Handle(ConfigurarAlertas requisicao, CancellationToken cancellationToken)
    {
        ConfiguracaoAlertasValidador.Validar(requisicao);

        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var config = await banco.Set<ConfiguracaoUsuario>().AsNoTracking().SingleAsync(x => x.UsuarioId == requisicao.UsuarioId, cancellationToken);
        banco.Attach(config);
        config.PercentualGastoAmarelo = requisicao.Amarelo;
        config.PercentualGastoVermelho = requisicao.Vermelho;
        config.PercentualCustosFixos = requisicao.Fixos;
        config.PercentualComprometimento = requisicao.Comprometimento;
        config.PercentualLimiteCartao = requisicao.Cartao;
        config.LimiteTransacoesEstabelecimento = requisicao.Estabelecimento;
        config.MultiplicadorDespesaAtipica = requisicao.Atipica;
        config.AlertasDesativados = requisicao.AlertasDesativados;

        await unidade.CommitAsync(transacao, cancellationToken);

        return config;
    }

    public async Task<ConfiguracaoUsuario> Handle(ConfigurarConfirmacao requisicao, CancellationToken cancellationToken)
    {
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var config = await banco.Set<ConfiguracaoUsuario>().AsNoTracking().SingleAsync(x => x.UsuarioId == requisicao.UsuarioId, cancellationToken);
        banco.Attach(config);
        config.ConfirmarAoVirarMes = requisicao.ConfirmarAoVirarMes;
        config.ConfirmacaoAutomaticaDesde = requisicao.ConfirmarAoVirarMes
            ? config.ConfirmacaoAutomaticaDesde ?? Mes(DateOnly.FromDateTime(DateTime.Now))
            : null;

        await unidade.CommitAsync(transacao, cancellationToken);

        return config;
    }
}
