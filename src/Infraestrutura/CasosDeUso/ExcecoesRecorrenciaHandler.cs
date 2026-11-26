using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class ExcecoesRecorrenciaHandler(FinanceiroDb banco, UnidadeFinanceira unidade)
    : IRequestHandler<AjustarRecorrencia, ExcecaoRecorrencia?>
{
    public async Task<ExcecaoRecorrencia?> Handle(AjustarRecorrencia requisicao, CancellationToken cancellationToken)
    {
        ExcecaoRecorrenciaValidador.Validar(requisicao);

        await using var transacao = await unidade.IniciarAsync(requisicao.UsuarioId, cancellationToken);
        var regra = await LeituraRecorrencias.ObterRegraAsync(unidade, requisicao.UsuarioId, requisicao.Id, requisicao.Competencia, cancellationToken);
        if (regra.Versao != requisicao.VersaoRegra)
            throw new ConcorrenciaException();
        if (await unidade.Ler<Lancamento>(requisicao.UsuarioId).AnyAsync(x => x.RegraRecorrenteId == regra.Id && x.Competencia == requisicao.Competencia, cancellationToken))
            throw new BusinessException("Esta ocorrência já foi confirmada. Use o extrato para corrigi-la.");
        var excecao = await unidade.Ler<ExcecaoRecorrencia>(requisicao.UsuarioId)
            .SingleOrDefaultAsync(x => x.RegraRecorrenteId == regra.Id && x.Competencia == requisicao.Competencia, cancellationToken);
        if (excecao?.Versao != requisicao.VersaoExcecao)
            throw new ConcorrenciaException();
        if (requisicao.Restaurar)
        {
            if (excecao is not null)
            {
                banco.Remove(excecao);
                unidade.Auditar<ExcecaoRecorrencia>(requisicao.UsuarioId, excecao.Id, "RestaurarPadrao", excecao, null);
            }
            await unidade.CommitAsync(transacao, cancellationToken);
            return null;
        }

        var antes = excecao is null ? null : banco.Entry(excecao).CurrentValues.ToObject() as ExcecaoRecorrencia;
        if (excecao is null)
        {
            excecao = new() { UsuarioId = requisicao.UsuarioId, RegraRecorrenteId = regra.Id, Competencia = requisicao.Competencia };
            banco.Add(excecao);
        }
        else
            banco.Attach(excecao);
        excecao.ValorOverride = requisicao.Valor;
        excecao.Ignorada = requisicao.Ignorada;
        unidade.Auditar(requisicao.UsuarioId, excecao.Id, antes is null ? "Criar" : "Atualizar", antes, excecao);
        await unidade.CommitAsync(transacao, cancellationToken);
        return excecao;
    }
}
