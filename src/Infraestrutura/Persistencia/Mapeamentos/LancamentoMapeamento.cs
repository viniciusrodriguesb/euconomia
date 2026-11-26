using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class LancamentoMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var lancamento = MapeamentoRegistro.Registrar<Lancamento>(modelo, "lancamento");
        MapeamentoRegistro.Vincular<Lancamento, Conta>(lancamento, nameof(Lancamento.ContaId));
        MapeamentoRegistro.Vincular<Lancamento, Cartao>(lancamento, nameof(Lancamento.CartaoId));
        MapeamentoRegistro.Vincular<Lancamento, Fatura>(lancamento, nameof(Lancamento.FaturaId));
        MapeamentoRegistro.Vincular<Lancamento, Categoria>(lancamento, nameof(Lancamento.CategoriaId));
        MapeamentoRegistro.Vincular<Lancamento, Estabelecimento>(lancamento, nameof(Lancamento.EstabelecimentoId));
        MapeamentoRegistro.Vincular<Lancamento, CompraParcelada>(lancamento, nameof(Lancamento.CompraParceladaId));
        MapeamentoRegistro.Vincular<Lancamento, RegraRecorrente>(lancamento, nameof(Lancamento.RegraRecorrenteId));
        MapeamentoRegistro.Vincular<Lancamento, Lancamento>(lancamento, nameof(Lancamento.LancamentoEstornadoId));
        lancamento.HasOne<Fatura>().WithMany()
            .HasForeignKey(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.FaturaId, lancamentoRegistro.CartaoId })
            .HasPrincipalKey(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, FaturaId = (Guid?)lancamentoRegistro.Id, CartaoId = (Guid?)lancamentoRegistro.CartaoId })
            .OnDelete(DeleteBehavior.Restrict);
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.ChaveIdempotencia }).IsUnique();
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.Competencia, lancamentoRegistro.Id });
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.DataLancamento, lancamentoRegistro.Id });
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.ContaId, lancamentoRegistro.DataLancamento });
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.EstabelecimentoId, lancamentoRegistro.Competencia });
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.CategoriaId, lancamentoRegistro.Competencia });
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.CompraParceladaId, lancamentoRegistro.NumeroParcela }).IsUnique();
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.RegraRecorrenteId, lancamentoRegistro.Competencia }).IsUnique();
        lancamento.HasIndex(lancamentoRegistro => new { lancamentoRegistro.UsuarioId, lancamentoRegistro.LancamentoEstornadoId }).IsUnique();
        lancamento.Property(lancamentoRegistro => lancamentoRegistro.Moeda).HasColumnType("char(3)").HasMaxLength(3);
        MapeamentoRegistro.AdicionarRestricao(lancamento, "valor_competencia", "valor > 0 AND moeda ~ '^[A-Z]{3}$' AND extract(day from competencia) = 1");
        MapeamentoRegistro.AdicionarRestricao(lancamento, "tipo_forma", "tipo BETWEEN 1 AND 7 AND forma_pagamento BETWEEN 1 AND 6 AND ((forma_pagamento = 6 AND tipo IN (2, 7) AND cartao_id IS NOT NULL AND fatura_id IS NOT NULL) OR (forma_pagamento <> 6 AND ((tipo IN (6, 7) AND cartao_id IS NOT NULL AND fatura_id IS NOT NULL) OR (tipo <> 6 AND cartao_id IS NULL AND fatura_id IS NULL))))");
        MapeamentoRegistro.AdicionarRestricao(lancamento, "categoria", "tipo NOT IN (1, 2, 3, 4) OR categoria_id IS NOT NULL");
        MapeamentoRegistro.AdicionarRestricao(lancamento, "estorno", "(tipo = 7) = (lancamento_estornado_id IS NOT NULL) AND lancamento_estornado_id IS DISTINCT FROM id");
        MapeamentoRegistro.AdicionarRestricao(lancamento, "parcelamento", "(compra_parcelada_id IS NULL AND numero_parcela IS NULL AND total_parcelas IS NULL) OR (tipo = 2 AND forma_pagamento = 6 AND compra_parcelada_id IS NOT NULL AND numero_parcela IS NOT NULL AND total_parcelas IS NOT NULL AND total_parcelas >= 2 AND numero_parcela BETWEEN 1 AND total_parcelas)");
    }
}
