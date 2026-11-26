using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class EstabelecimentosHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<RegistrarEstabelecimento, Estabelecimento>,
      IRequestHandler<AssociarAlias, AliasEstabelecimento>,
      IRequestHandler<MesclarEstabelecimento>,
      IRequestHandler<AjustarLimiteEstabelecimento, Estabelecimento>
{
    public async Task<Estabelecimento> Handle(RegistrarEstabelecimento requisicao, CancellationToken cancellationToken)
    {
        var (nome, normalizado) = EstabelecimentoValidador.ValidarCadastro(requisicao.Dados);

        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var existentes = unidade.Ler<Estabelecimento>(requisicao.UsuarioId);
        var existente = await existentes.SingleOrDefaultAsync(x => x.NomeNormalizado == normalizado, cancellationToken);
        if (existente is not null)
            return existente.MescladoEmId is { } destino ? await existentes.SingleAsync(x => x.Id == destino, cancellationToken) : existente;
        var alias = await unidade.Ler<AliasEstabelecimento>(requisicao.UsuarioId).SingleOrDefaultAsync(x => x.NomeNormalizado == normalizado, cancellationToken);
        if (alias is not null)
            return await existentes.SingleAsync(x => x.Id == alias.EstabelecimentoId, cancellationToken);
        var loja = new Estabelecimento { UsuarioId = requisicao.UsuarioId, Nome = nome, NomeNormalizado = normalizado, LimiteTransacoes = requisicao.Dados.LimiteTransacoes };

        banco.Add(loja);

        await unidade.CommitAsync(transacao, cancellationToken);

        return loja;
    }

    public async Task<AliasEstabelecimento> Handle(AssociarAlias requisicao, CancellationToken cancellationToken)
    {
        var normalizado = EstabelecimentoValidador.ValidarAlias(requisicao.Nome);

        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var loja = await unidade.ObterAsync<Estabelecimento>(requisicao.UsuarioId, requisicao.Id, "Estabelecimento não encontrado.", cancellationToken);
        if (loja.MescladoEmId is not null)
            throw new BusinessException("Selecione o estabelecimento principal.");
        if (await unidade.Ler<Estabelecimento>(requisicao.UsuarioId).AnyAsync(x => x.NomeNormalizado == normalizado, cancellationToken) ||
            await unidade.Ler<AliasEstabelecimento>(requisicao.UsuarioId).AnyAsync(x => x.NomeNormalizado == normalizado, cancellationToken))
            throw new BusinessException("Esse nome já existe. Use a mesclagem para unir dois estabelecimentos cadastrados.");
        var alias = new AliasEstabelecimento { UsuarioId = requisicao.UsuarioId, EstabelecimentoId = loja.Id, NomeNormalizado = normalizado };

        banco.Add(alias);

        await unidade.CommitAsync(transacao, cancellationToken);

        return alias;
    }

    public async Task Handle(MesclarEstabelecimento requisicao, CancellationToken cancellationToken)
    {
        if (requisicao.Origem == requisicao.Destino)
            throw new BusinessException("Selecione dois estabelecimentos diferentes.");
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var origem = await unidade.ObterAsync<Estabelecimento>(requisicao.UsuarioId, requisicao.Origem, "Estabelecimento de origem não encontrado.", cancellationToken);
        var destino = await unidade.ObterAsync<Estabelecimento>(requisicao.UsuarioId, requisicao.Destino, "Estabelecimento de destino não encontrado.", cancellationToken);
        if (origem.MescladoEmId is not null || destino.MescladoEmId is not null)
            throw new BusinessException("Selecione dois estabelecimentos principais.");
        var associados = await unidade.Ler<Estabelecimento>(requisicao.UsuarioId).Where(x => x.MescladoEmId == origem.Id).ToArrayAsync(cancellationToken);
        banco.AttachRange(associados);
        banco.Attach(origem);
        foreach (var loja in associados.Append(origem))
            loja.MescladoEmId = destino.Id;
        var aliases = await unidade.Ler<AliasEstabelecimento>(requisicao.UsuarioId).Where(x => x.EstabelecimentoId == origem.Id).ToArrayAsync(cancellationToken);
        banco.AttachRange(aliases);
        foreach (var alias in aliases)
            alias.EstabelecimentoId = destino.Id;

        await unidade.CommitAsync(transacao, cancellationToken);
    }

    public async Task<Estabelecimento> Handle(AjustarLimiteEstabelecimento requisicao, CancellationToken cancellationToken)
    {
        EstabelecimentoValidador.ValidarLimite(requisicao.Limite);

        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var loja = await unidade.ObterAsync<Estabelecimento>(requisicao.UsuarioId, requisicao.Id, "Estabelecimento não encontrado.", cancellationToken);
        if (loja.MescladoEmId.HasValue)
            throw new BusinessException("Configure o limite no estabelecimento principal.");
        banco.Attach(loja);
        loja.LimiteTransacoes = requisicao.Limite;

        await unidade.CommitAsync(transacao, cancellationToken);

        return loja;
    }

}
