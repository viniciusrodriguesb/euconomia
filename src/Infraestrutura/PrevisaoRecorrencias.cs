using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

internal static class PrevisaoRecorrencias
{
    public static IQueryable<RegraRecorrente> Vigentes(UnidadeFinanceira unidade, Guid usuarioId)
    {
        var mesAtual = Mes(DateOnly.FromDateTime(DateTime.Now));

        return unidade.Ler<RegraRecorrente>(usuarioId).Where(regra => regra.CompetenciaFim == null ||
            regra.CompetenciaFim >= mesAtual && regra.CompetenciaFim >= regra.CompetenciaInicio);
    }

    public static async Task<OcorrenciaPrevista[]> ListarAsync(UnidadeFinanceira unidade, ListarPrevisao request, CancellationToken ct)
    {
        var inicio = request.Inicio;
        var fim = PeriodoPrevisaoValidador.CalcularFim(inicio, request.Meses);
        var regras = await unidade.Ler<RegraRecorrente>(request.UsuarioId)
            .Where(x => x.CompetenciaInicio <= fim && (x.CompetenciaFim == null || x.CompetenciaFim >= inicio)).ToArrayAsync(ct);
        var excecoes = await unidade.Ler<ExcecaoRecorrencia>(request.UsuarioId)
            .Where(x => x.Competencia >= inicio && x.Competencia <= fim).ToDictionaryAsync(x => (x.RegraRecorrenteId, x.Competencia), ct);
        var realizados = await unidade.Ler<Lancamento>(request.UsuarioId)
            .Where(x => x.RegraRecorrenteId.HasValue && x.Competencia >= inicio && x.Competencia <= fim)
            .Select(x => new { Id = x.RegraRecorrenteId!.Value, x.Competencia }).ToListAsync(ct);
        var chaves = realizados.Select(x => (x.Id, x.Competencia)).ToHashSet();
        var resultado = new List<OcorrenciaPrevista>();
        foreach (var regra in regras)
        {
            var ultimo = regra.CompetenciaFim is { } limite && limite < fim ? limite : fim;
            var mes = regra.CompetenciaInicio > inicio ? regra.CompetenciaInicio : inicio;
            while (mes <= ultimo)
            {
                ct.ThrowIfCancellationRequested();
                excecoes.TryGetValue((regra.Id, mes), out var excecao);
                resultado.Add(Projecao(regra, excecao, mes, chaves.Contains((regra.Id, mes))));
                if (mes == ultimo)
                    break;
                mes = mes.AddMonths(1);
            }
        }
        return resultado.OrderBy(x => x.Competencia).ThenBy(x => x.Data).ThenBy(x => x.RegraId).ToArray();
    }

    public static OcorrenciaPrevista Projecao(RegraRecorrente regra, ExcecaoRecorrencia? excecao, DateOnly mes, bool realizada) =>
        new(regra.Id, regra.Versao, excecao?.Versao, mes, Dia(mes, regra.DiaDoMes), regra.Tipo, regra.Descricao,
            excecao?.Ignorada == true ? 0 : excecao?.ValorOverride ?? regra.ValorBase,
            excecao is not null, excecao?.Ignorada == true, realizada, regra.FormaPagamento, regra.ContaId, regra.CartaoId, regra.CategoriaId);
}
