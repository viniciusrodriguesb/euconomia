using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class RegraRecorrenteMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var regra = MapeamentoRegistro.Registrar<RegraRecorrente>(modelo, "regra_recorrente");
        MapeamentoRegistro.Vincular<RegraRecorrente, Conta>(regra, nameof(RegraRecorrente.ContaId));
        MapeamentoRegistro.Vincular<RegraRecorrente, Cartao>(regra, nameof(RegraRecorrente.CartaoId));
        MapeamentoRegistro.Vincular<RegraRecorrente, Categoria>(regra, nameof(RegraRecorrente.CategoriaId));
        MapeamentoRegistro.Vincular<RegraRecorrente, RegraRecorrente>(regra, nameof(RegraRecorrente.RegraAnteriorId));
        regra.HasIndex(regraRegistro => new { regraRegistro.UsuarioId, regraRegistro.CompetenciaInicio });
        regra.HasIndex(regraRegistro => new { regraRegistro.UsuarioId, regraRegistro.RegraAnteriorId }).IsUnique();
        regra.Property(regraRegistro => regraRegistro.Versao).IsRowVersion();
        MapeamentoRegistro.AdicionarRestricao(regra, "valor_dia", "valor_base > 0 AND dia_do_mes BETWEEN 1 AND 31");
        MapeamentoRegistro.AdicionarRestricao(regra, "periodo", "extract(day from competencia_inicio) = 1 AND (competencia_fim IS NULL OR (extract(day from competencia_fim) = 1 AND competencia_fim >= (competencia_inicio - interval '1 month')::date))");
        MapeamentoRegistro.AdicionarRestricao(regra, "tipo_forma", "tipo BETWEEN 1 AND 4 AND forma_pagamento BETWEEN 1 AND 6 AND (tipo = 4) = (forma_pagamento = 6) AND (forma_pagamento = 6) = (cartao_id IS NOT NULL)");
    }
}
