using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class ExcecaoRecorrenciaMapeamento
{
    public static void Configurar(ModelBuilder modelo)
    {
        var excecao = MapeamentoRegistro.Registrar<ExcecaoRecorrencia>(modelo, "excecao_recorrencia");
        MapeamentoRegistro.Vincular<ExcecaoRecorrencia, RegraRecorrente>(excecao, nameof(ExcecaoRecorrencia.RegraRecorrenteId));
        excecao.HasIndex(excecaoRegistro => new { excecaoRegistro.UsuarioId, excecaoRegistro.RegraRecorrenteId, excecaoRegistro.Competencia }).IsUnique();
        excecao.Property(excecaoRegistro => excecaoRegistro.Versao).IsRowVersion();
        MapeamentoRegistro.AdicionarRestricao(excecao, "valor", "extract(day from competencia) = 1 AND ((ignorada AND valor_override IS NULL) OR (NOT ignorada AND valor_override IS NOT NULL AND valor_override > 0))");
    }
}
