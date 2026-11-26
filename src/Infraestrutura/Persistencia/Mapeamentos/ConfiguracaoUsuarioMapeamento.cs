using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class ConfiguracaoUsuarioMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var config = modelo.Entity<ConfiguracaoUsuario>().ToTable("configuracao_usuario");
        config.HasKey(configuracaoRegistro => configuracaoRegistro.UsuarioId);
        config.HasOne<Usuario>().WithOne().HasForeignKey<ConfiguracaoUsuario>(configuracaoRegistro => configuracaoRegistro.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        MapeamentoRegistro.AdicionarRestricao(config, "limites", "percentual_gasto_amarelo > 0 AND percentual_gasto_vermelho >= percentual_gasto_amarelo AND percentual_custos_fixos > 0 AND percentual_comprometimento > 0 AND percentual_limite_cartao > 0 AND limite_transacoes_estabelecimento > 0 AND multiplicador_despesa_atipica > 0");
        config.HasIndex(configuracaoRegistro => new { configuracaoRegistro.UsuarioId, configuracaoRegistro.ConfirmacaoAutomaticaDesde }).HasFilter("confirmar_ao_virar_mes AND confirmacao_automatica_desde IS NOT NULL");
        MapeamentoRegistro.AdicionarRestricao(config, "alertas", "alertas_desativados BETWEEN 0 AND 255");
        MapeamentoRegistro.AdicionarRestricao(config, "confirmacao", "confirmacao_automatica_desde IS NULL OR (confirmar_ao_virar_mes AND extract(day from confirmacao_automatica_desde) = 1)");
    }
}
