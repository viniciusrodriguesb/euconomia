using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class AlertaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var alerta = MapeamentoRegistro.Registrar<Alerta>(modelo, "alerta");
        alerta.HasIndex(alertaRegistro => new { alertaRegistro.UsuarioId, alertaRegistro.Tipo, alertaRegistro.Competencia, alertaRegistro.Referencia }).IsUnique();
        MapeamentoRegistro.AdicionarRestricao(alerta, "tipo_gravidade_competencia", "tipo BETWEEN 1 AND 8 AND gravidade BETWEEN 1 AND 2 AND extract(day from competencia) = 1");
    }
}
