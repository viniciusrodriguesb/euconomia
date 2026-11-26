using Aplicacao;
using Dominio;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

internal static class EstornoValidador
{
    public static async Task ValidarOriginalAsync(UnidadeFinanceira unidade, EstornarLancamento requisicao,
        Lancamento original, CancellationToken cancellationToken)
    {
        if (original.Tipo == TipoLancamento.Estorno ||
            await unidade.Ler<Lancamento>(requisicao.UsuarioId).AnyAsync(x => x.LancamentoEstornadoId == original.Id, cancellationToken))
            throw new BusinessException("O lançamento não pode ser estornado novamente.");
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        if (requisicao.Data == default || requisicao.Data > hoje || requisicao.Data < original.DataLancamento)
            throw new BusinessException("Informe uma data de estorno válida até hoje.");
    }

    public static void ValidarSubstituto(EstornarLancamento requisicao, DadosLancamento substituto)
    {
        if (substituto.ChaveIdempotencia == Guid.Empty || substituto.ChaveIdempotencia == requisicao.ChaveIdempotencia)
            throw new BusinessException("Gere uma chave de idempotência distinta para o lançamento substituto.");
    }
}
