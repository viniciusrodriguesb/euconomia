using Aplicacao;
using Dominio;

namespace Infraestrutura;

internal static class LeituraRecorrencias
{
    public static async Task<RegraRecorrente> ObterRegraAsync(UnidadeFinanceira unidade, Guid usuarioId, Guid id, DateOnly mes,
        CancellationToken cancellationToken)
    {
        var regra = await unidade.ObterAsync<RegraRecorrente>(usuarioId, id, "Regra não encontrada.", cancellationToken);
        RegraRecorrenteValidador.ValidarCompetencia(regra, mes);
        return regra;
    }
}
