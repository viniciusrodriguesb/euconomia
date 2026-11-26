using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class CategoriasCadastroHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<RegistrarCategoria, Categoria>
{
    public async Task<Categoria> Handle(RegistrarCategoria requisicao, CancellationToken cancellationToken)
    {
        var categoria = new Categoria { UsuarioId = requisicao.UsuarioId, Nome = NomeCadastroValidador.Validar(requisicao.Nome) };
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        if (await unidade.Ler<Categoria>(requisicao.UsuarioId).AnyAsync(x => x.Nome == categoria.Nome && x.InativadoEm == null, cancellationToken))
            throw new BusinessException("Já existe uma categoria ativa com este nome.");

        banco.Add(categoria);

        await unidade.CommitAsync(transacao, cancellationToken);

        return categoria;
    }
}
