using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class RegistroIdempotenciaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var idempotencia = MapeamentoRegistro.Registrar<RegistroIdempotencia>(modelo, "registro_idempotencia");
        idempotencia.HasIndex(idempotenciaRegistro => new { idempotenciaRegistro.UsuarioId, idempotenciaRegistro.ChaveIdempotencia }).IsUnique();
        idempotencia.Property(idempotenciaRegistro => idempotenciaRegistro.Resposta).HasColumnType("jsonb");
    }
}
