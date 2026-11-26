using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class AuditoriaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var auditoria = MapeamentoRegistro.Registrar<Auditoria>(modelo, "auditoria");
        auditoria.HasIndex(auditoriaRegistro => new { auditoriaRegistro.UsuarioId, auditoriaRegistro.EntidadeId, auditoriaRegistro.CriadoEm });
        auditoria.Property(auditoriaRegistro => auditoriaRegistro.Antes).HasColumnType("jsonb");
        auditoria.Property(auditoriaRegistro => auditoriaRegistro.Depois).HasColumnType("jsonb");
        MapeamentoRegistro.AdicionarRestricao(auditoria, "conteudo", "antes IS NOT NULL OR depois IS NOT NULL");
    }
}
