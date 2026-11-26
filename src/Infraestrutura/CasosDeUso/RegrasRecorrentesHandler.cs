using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Aplicacao.CalculoFinanceiro;

namespace Infraestrutura;

public sealed class RegrasRecorrentesHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<RegistrarRegra, RegraRecorrente>,
      IRequestHandler<VersionarRegra, RegraRecorrente>,
      IRequestHandler<EncerrarRegra, RegraRecorrente>
{
    public async Task<RegraRecorrente> Handle(RegistrarRegra requisicao, CancellationToken cancellationToken)
    {
        var dados = requisicao.Dados;
        RegraRecorrenteValidador.ValidarCadastro(dados);

        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var conta = await unidade.ObterAsync<Conta>(requisicao.UsuarioId, dados.ContaId, "Conta não encontrada.", cancellationToken);
        var categoria = await unidade.ObterAsync<Categoria>(requisicao.UsuarioId, dados.CategoriaId, "Categoria não encontrada.", cancellationToken);
        if (conta.InativadoEm.HasValue || categoria.InativadoEm.HasValue ||
            Dia(dados.CompetenciaInicio, dados.DiaDoMes) < conta.DataSaldoInicial)
            throw new BusinessException("Selecione cadastros ativos e um início a partir do saldo inicial.");
        if (dados.CartaoId is { } id)
        {
            var cartao = await unidade.ObterAsync<Cartao>(requisicao.UsuarioId, id, "Cartão não encontrado.", cancellationToken);
            if (cartao.InativadoEm.HasValue || cartao.ContaPagamentoId != conta.Id)
                throw new BusinessException("Selecione um cartão ativo e sua conta de pagamento.");
        }

        var regra = new RegraRecorrente
        {
            UsuarioId = requisicao.UsuarioId,
            Tipo = dados.Tipo,
            ValorBase = dados.ValorBase,
            DiaDoMes = dados.DiaDoMes,
            CompetenciaInicio = dados.CompetenciaInicio,
            CompetenciaFim = dados.CompetenciaFim,
            FormaPagamento = dados.FormaPagamento,
            ContaId = dados.ContaId,
            CategoriaId = dados.CategoriaId,
            CartaoId = dados.CartaoId,
            Descricao = dados.Descricao.Trim()
        };
        banco.Add(regra);
        unidade.Auditar(requisicao.UsuarioId, regra.Id, "Criar", null, regra);
        await unidade.CommitAsync(transacao, cancellationToken);
        return regra;
    }

    public async Task<RegraRecorrente> Handle(VersionarRegra requisicao, CancellationToken cancellationToken)
    {
        ValorMonetarioValidador.Validar(requisicao.Valor);
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var regra = await unidade.ObterAsync<RegraRecorrente>(requisicao.UsuarioId, requisicao.Id, "Regra não encontrada.", cancellationToken);
        if (requisicao.Versao != regra.Versao)
            throw new ConcorrenciaException();
        RegraRecorrenteValidador.ValidarCompetencia(regra, requisicao.Competencia);
        if (requisicao.Competencia.Year < 2 ||
            await unidade.Ler<Lancamento>(requisicao.UsuarioId).AnyAsync(x => x.RegraRecorrenteId == regra.Id && x.Competencia >= requisicao.Competencia, cancellationToken) ||
            await unidade.Ler<RegraRecorrente>(requisicao.UsuarioId).AnyAsync(x => x.RegraAnteriorId == regra.Id, cancellationToken))
            throw new BusinessException("Escolha um mês sem ocorrências já confirmadas ou versão sucessora.");

        var anterior = banco.Entry(regra).CurrentValues.ToObject() as RegraRecorrente;
        var nova = new RegraRecorrente
        {
            UsuarioId = regra.UsuarioId,
            RegraAnteriorId = regra.Id,
            Tipo = regra.Tipo,
            ValorBase = requisicao.Valor,
            DiaDoMes = regra.DiaDoMes,
            CompetenciaInicio = requisicao.Competencia,
            CompetenciaFim = regra.CompetenciaFim,
            FormaPagamento = regra.FormaPagamento,
            ContaId = regra.ContaId,
            CategoriaId = regra.CategoriaId,
            CartaoId = regra.CartaoId,
            Descricao = regra.Descricao
        };
        banco.Attach(regra);
        regra.CompetenciaFim = requisicao.Competencia.AddMonths(-1);
        banco.Add(nova);
        unidade.Auditar(requisicao.UsuarioId, regra.Id, "EncerrarVersao", anterior, regra);
        unidade.Auditar(requisicao.UsuarioId, nova.Id, "CriarVersao", null, nova);
        await TransferirExcecoesAsync(requisicao, regra.Id, nova.Id, cancellationToken);

        await unidade.CommitAsync(transacao, cancellationToken);
        return nova;
    }

    public async Task<RegraRecorrente> Handle(EncerrarRegra requisicao, CancellationToken cancellationToken)
    {
        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var regra = await unidade.ObterAsync<RegraRecorrente>(requisicao.UsuarioId, requisicao.Id, "Regra não encontrada.", cancellationToken);
        if (regra.Versao != requisicao.Versao)
            throw new ConcorrenciaException();
        RegraRecorrenteValidador.ValidarCompetencia(regra, requisicao.Competencia);
        if (requisicao.Competencia.Year < 2 || await unidade.Ler<Lancamento>(requisicao.UsuarioId)
            .AnyAsync(x => x.RegraRecorrenteId == regra.Id && x.Competencia >= requisicao.Competencia, cancellationToken))
            throw new BusinessException("Escolha um mês a partir do qual nenhuma ocorrência desta regra tenha sido confirmada.");
        var antes = (RegraRecorrente)banco.Entry(regra).CurrentValues.ToObject();
        banco.Attach(regra);
        regra.CompetenciaFim = requisicao.Competencia.AddMonths(-1);
        unidade.Auditar(requisicao.UsuarioId, regra.Id, "Encerrar", antes, regra);
        await unidade.CommitAsync(transacao, cancellationToken);
        return regra;
    }

    private async Task TransferirExcecoesAsync(VersionarRegra requisicao, Guid regraAnteriorId, Guid regraNovaId, CancellationToken cancellationToken)
    {
        var excecoes = await unidade.Ler<ExcecaoRecorrencia>(requisicao.UsuarioId)
            .Where(x => x.RegraRecorrenteId == regraAnteriorId && x.Competencia >= requisicao.Competencia).ToArrayAsync(cancellationToken);
        foreach (var excecao in excecoes)
        {
            if (excecao.Competencia == requisicao.Competencia)
            {
                banco.Remove(excecao);
                unidade.Auditar<ExcecaoRecorrencia>(requisicao.UsuarioId, excecao.Id, "AplicarNovoPadrao", excecao, null);
                continue;
            }
            var antes = banco.Entry(excecao).CurrentValues.ToObject() as ExcecaoRecorrencia;
            banco.Attach(excecao);
            excecao.RegraRecorrenteId = regraNovaId;
            unidade.Auditar(requisicao.UsuarioId, excecao.Id, "TransferirParaNovaVersao", antes, excecao);
        }
    }
}
