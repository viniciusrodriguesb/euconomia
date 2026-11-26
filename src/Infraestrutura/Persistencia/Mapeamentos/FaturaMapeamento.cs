using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class FaturaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var fatura = MapeamentoRegistro.Registrar<Fatura>(modelo, "fatura");
        fatura.HasAlternateKey(faturaRegistro => new { faturaRegistro.UsuarioId, faturaRegistro.Id, faturaRegistro.CartaoId });
        fatura.HasIndex(faturaRegistro => new { faturaRegistro.UsuarioId, faturaRegistro.CartaoId, faturaRegistro.Competencia }).IsUnique();
        fatura.HasIndex(faturaRegistro => new { faturaRegistro.UsuarioId, faturaRegistro.Competencia });
        fatura.HasIndex(faturaRegistro => new { faturaRegistro.UsuarioId, faturaRegistro.LancamentoPagamentoId }).IsUnique();
        fatura.Property(faturaRegistro => faturaRegistro.Versao).IsRowVersion();
        MapeamentoRegistro.Vincular<Fatura, Cartao>(fatura, nameof(Fatura.CartaoId));
        MapeamentoRegistro.Vincular<Fatura, Lancamento>(fatura, nameof(Fatura.LancamentoPagamentoId));
        MapeamentoRegistro.AdicionarRestricao(fatura, "datas", "extract(day from competencia) = 1 AND date_trunc('month', data_fechamento)::date = competencia AND data_vencimento > data_fechamento");
        MapeamentoRegistro.AdicionarRestricao(fatura, "status", "status BETWEEN 1 AND 3 AND (status = 3) = (lancamento_pagamento_id IS NOT NULL)");
    }
}
