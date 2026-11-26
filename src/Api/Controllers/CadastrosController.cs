using Api.Contratos;
using Dominio;
using Infraestrutura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[Route("api/cadastros")]
public sealed class CadastrosController(UnidadeFinanceira unidade) : ApiControllerBase
{
    [HttpGet]
    public async Task<CadastrosResposta> Listar(CancellationToken cancellationToken)
    {
        var usuarioId = UsuarioId;

        var contas = await unidade.Ler<Conta>(usuarioId).OrderBy(conta => conta.Nome).ToArrayAsync(cancellationToken);
        var categorias = await unidade.Ler<Categoria>(usuarioId).OrderBy(categoria => categoria.Nome).ToArrayAsync(cancellationToken);
        var cartoes = await unidade.Ler<Cartao>(usuarioId).OrderBy(cartao => cartao.Nome).ToArrayAsync(cancellationToken);
        var estabelecimentos = await unidade.Ler<Estabelecimento>(usuarioId)
            .Where(estabelecimento => estabelecimento.MescladoEmId == null)
            .OrderBy(estabelecimento => estabelecimento.Nome)
            .ToArrayAsync(cancellationToken);

        return new(contas, categorias, cartoes, estabelecimentos);
    }
}
