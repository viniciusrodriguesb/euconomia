using System.Data;
using Aplicacao;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class PrevisaoPaginadaHandler(FinanceiroDb banco) : IRequestHandler<ListarPrevisaoPaginada, Pagina<OcorrenciaPrevista>>
{
    public async Task<Pagina<OcorrenciaPrevista>> Handle(ListarPrevisaoPaginada requisicao, CancellationToken cancellationToken)
    {
        var inicio = requisicao.Inicio;
        var fim = PeriodoPrevisaoValidador.CalcularFim(inicio, requisicao.Meses);
        var consulta = banco.Database.SqlQuery<OcorrenciaPrevista>($"""
            SELECT regra.id AS "RegraId", regra.xmin::text::bigint AS "VersaoRegra",
                excecao.xmin::text::bigint AS "VersaoExcecao",
                competencia.mes::date AS "Competencia",
                competencia.mes::date + (least(regra.dia_do_mes,
                    extract(day FROM (competencia.mes + interval '1 month - 1 day'))::integer) - 1) AS "Data",
                regra.tipo AS "Tipo", regra.descricao AS "Descricao",
                CASE WHEN excecao.ignorada THEN 0 ELSE coalesce(excecao.valor_override, regra.valor_base) END AS "Valor",
                excecao.id IS NOT NULL AS "Ajustada",
                coalesce(excecao.ignorada, false) AS "Ignorada",
                EXISTS (
                    SELECT 1 FROM lancamento realizado
                    WHERE realizado.usuario_id = regra.usuario_id AND realizado.regra_recorrente_id = regra.id
                        AND realizado.competencia = competencia.mes::date
                ) AS "Realizada",
                regra.forma_pagamento AS "FormaPagamento", regra.conta_id AS "ContaId",
                regra.cartao_id AS "CartaoId", regra.categoria_id AS "CategoriaId"
            FROM regra_recorrente regra
            CROSS JOIN LATERAL generate_series(
                greatest(regra.competencia_inicio, {inicio})::timestamp,
                least(coalesce(regra.competencia_fim, {fim}), {fim})::timestamp,
                interval '1 month'
            ) AS competencia(mes)
            LEFT JOIN excecao_recorrencia excecao
                ON excecao.usuario_id = regra.usuario_id AND excecao.regra_recorrente_id = regra.id
                AND excecao.competencia = competencia.mes::date
            WHERE regra.usuario_id = {requisicao.UsuarioId} AND regra.competencia_inicio <= {fim}
                AND (regra.competencia_fim IS NULL OR regra.competencia_fim >= {inicio})
            """);

        await using var leitura = await banco.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var pagina = await consulta.OrderBy(ocorrencia => ocorrencia.Competencia)
            .ThenBy(ocorrencia => ocorrencia.Data)
            .ThenBy(ocorrencia => ocorrencia.RegraId)
            .PaginarAsync(requisicao.Pagina, requisicao.Tamanho, cancellationToken);
        await leitura.CommitAsync(cancellationToken);

        return pagina;
    }
}
