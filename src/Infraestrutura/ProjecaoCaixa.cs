using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

internal static class ProjecaoCaixa
{
    public static async Task<ProjecaoMes[]> CalcularAsync(FinanceiroDb db, UnidadeFinanceira unidade,
        Guid usuario, DateOnly hoje, int horizonte, decimal saldo, CancellationToken ct)
    {
        var atual = Mes(hoje);
        var limite = atual.AddMonths(horizonte);
        var regras = await PrevisaoRecorrencias.ListarAsync(unidade, new ListarPrevisao(usuario, atual, horizonte), ct);
        var cartoes = await unidade.Ler<Cartao>(usuario).ToDictionaryAsync(x => x.Id, ct);
        var contas = await unidade.Ler<Conta>(usuario).Where(x => x.InativadoEm == null).Select(x => x.Id).ToArrayAsync(ct);
        var previstas = regras.Where(x => !x.Realizada && !x.Ignorada && contas.Contains(x.ContaId)).ToLookup(x => x.Competencia);
        var pagamentos = await db.Database.SqlQuery<FaturaPrevistaLinha>($"""
            SELECT f.id AS "Id", f.data_vencimento AS "Vencimento", t.total AS "Total"
            FROM fatura f JOIN total_fatura t ON t.usuario_id = f.usuario_id AND t.fatura_id = f.id
            WHERE f.usuario_id = {usuario} AND f.status <> {(int)StatusFatura.Paga} AND f.data_vencimento < {limite}
            """).ToArrayAsync(ct);
        var valoresFaturas = pagamentos.ToLookup(x => x.Vencimento < atual ? atual : Mes(x.Vencimento));
        var calendario = await unidade.Ler<Fatura>(usuario).Where(x => x.Competencia >= atual && x.Competencia < limite)
            .ToDictionaryAsync(x => (x.CartaoId, x.Competencia), ct);
        var assinaturas = previstas.SelectMany(x => x).Where(x => x.Tipo == TipoRecorrencia.AssinaturaCartao &&
            x.CartaoId.HasValue && !cartoes[x.CartaoId.Value].InativadoEm.HasValue)
            .ToLookup(x =>
            {
                var cartao = cartoes[x.CartaoId!.Value];
                var fechamento = calendario.TryGetValue((cartao.Id, x.Competencia), out var faturaAtual)
                    ? faturaAtual.DataFechamento : Dia(x.Competencia, cartao.DiaFechamento);
                var mesFatura = x.Data > fechamento ? x.Competencia.AddMonths(1) : x.Competencia;
                if (calendario.TryGetValue((cartao.Id, mesFatura), out var fatura))
                    return Mes(fatura.DataVencimento);
                fechamento = Dia(mesFatura, cartao.DiaFechamento);
                var vencimento = Dia(mesFatura, cartao.DiaVencimento);
                return vencimento > fechamento ? mesFatura : mesFatura.AddMonths(1);
            });
        var ajustados = regras.Where(x => x.Ajustada).Select(x => x.Competencia).ToHashSet();
        var resultado = new ProjecaoMes[horizonte];
        for (var i = 0; i < horizonte; i++)
        {
            var mes = atual.AddMonths(i);
            var itens = previstas[mes];
            var entradas = itens.Where(x => x.Tipo == TipoRecorrencia.ReceitaFixa).Sum(x => x.Valor);
            var aportes = itens.Where(x => x.Tipo == TipoRecorrencia.AporteFixo).Sum(x => x.Valor);
            var fixos = itens.Where(x => x.Tipo == TipoRecorrencia.CustoFixo).Sum(x => x.Valor);
            var faturas = valoresFaturas[mes].Sum(x => x.Total) + assinaturas[mes].Sum(x => x.Valor);
            saldo += entradas - fixos - aportes - faturas;
            resultado[i] = new(mes, entradas, fixos, faturas, aportes, saldo, ajustados.Contains(mes));
        }
        return resultado;
    }

}
