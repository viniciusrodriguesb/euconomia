using System.Security.Cryptography;
using System.Text;
using Aplicacao;
using Dominio;
using Infraestrutura;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Servicos;

public sealed class ConfirmacoesAutomaticas(IServiceScopeFactory escopos, ILogger<ConfirmacoesAutomaticas> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await ProcessarAsync(DateOnly.FromDateTime(DateTime.Now), ct); }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                logger.LogError(e, "A confirmação automática será retomada no próximo ciclo.");
            }
        }
    }

    public async Task ProcessarAsync(DateOnly hoje, CancellationToken ct)
    {
        var mesAtual = CalculoFinanceiro.Mes(hoje);
        // O cursor avança durante a execução. Sempre consulte a primeira página ainda pendente.
        var falhas = new HashSet<Guid>();
        while (!ct.IsCancellationRequested)
        {
            await using var escopo = escopos.CreateAsyncScope();
            var db = escopo.ServiceProvider.GetRequiredService<FinanceiroDb>();
            var lote = await db.Set<ConfiguracaoUsuario>().AsNoTracking()
                .Where(x => x.ConfirmarAoVirarMes && x.ConfirmacaoAutomaticaDesde < mesAtual && !falhas.Contains(x.UsuarioId))
                .OrderBy(x => x.UsuarioId).Take(100).ToArrayAsync(ct);
            if (lote.Length == 0)
                return;

            foreach (var config in lote)
            {
                var mes = config.ConfirmacaoAutomaticaDesde!.Value;
                try { await ProcessarMesAsync(config.UsuarioId, mes, ct); }
                catch (Exception e) when (!ct.IsCancellationRequested)
                {
                    falhas.Add(config.UsuarioId);
                    logger.LogWarning(e, "Há ocorrências pendentes de confirmação automática em {Competencia}.", mes);
                }
            }
        }
    }

    private async Task ProcessarMesAsync(Guid usuario, DateOnly mes, CancellationToken ct)
    {
        await using var leitura = escopos.CreateAsyncScope();
        var services = leitura.ServiceProvider;
        var previstos = await services.GetRequiredService<ISender>().Send(new ListarPrevisao(usuario, mes, 1), ct);
        foreach (var item in previstos.Where(x => !x.Ignorada && !x.Realizada))
        {
            await using var gravacao = escopos.CreateAsyncScope();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"recorrencia:{usuario:D}:{item.RegraId:D}:{mes:yyyy-MM}"));
            var chave = new Guid(hash.AsSpan(0, 16));
            await gravacao.ServiceProvider.GetRequiredService<ISender>()
                .Send(new ConfirmarRecorrencia(usuario, item.RegraId, mes, chave, Automatica: true), ct);
        }

        var unidade = services.GetRequiredService<UnidadeFinanceira>();
        var db = services.GetRequiredService<FinanceiroDb>();
        await using var transacao = await unidade.IniciarAsync(usuario, ct);
        var config = await db.Set<ConfiguracaoUsuario>().AsNoTracking().SingleAsync(x => x.UsuarioId == usuario, ct);
        if (config.ConfirmarAoVirarMes && config.ConfirmacaoAutomaticaDesde == mes)
        {
            var pendentes = await services.GetRequiredService<ISender>().Send(new ListarPrevisao(usuario, mes, 1), ct);
            if (pendentes.Any(x => !x.Ignorada && !x.Realizada))
                return;
            db.Attach(config);
            config.ConfirmacaoAutomaticaDesde = mes.AddMonths(1);
        }
        await unidade.CommitAsync(transacao, ct);
    }
}
