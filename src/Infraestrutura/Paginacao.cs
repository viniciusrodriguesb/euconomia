using Aplicacao;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public static class Paginacao
{
    public static async Task<Pagina<T>> PaginarAsync<T>(this IOrderedQueryable<T> consulta, int pagina, int tamanho, CancellationToken ct)
    {
        if (pagina < 1 || tamanho is < 1 or > 200 || (long)(pagina - 1) * tamanho > int.MaxValue)
            throw new BusinessException("Informe uma página válida e até 200 registros por página.");
        var total = await consulta.CountAsync(ct);
        return new(await consulta.Skip((pagina - 1) * tamanho).Take(tamanho).ToArrayAsync(ct), pagina, tamanho, total);
    }
}
