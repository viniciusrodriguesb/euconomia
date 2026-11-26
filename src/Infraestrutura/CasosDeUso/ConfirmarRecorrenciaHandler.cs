using Aplicacao;
using Dominio;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura;

public sealed class ConfirmarRecorrenciaHandler(FinanceiroDb banco, UnidadeFinanceira unidade, RegistroLancamentos registro, AlertasFinanceiros alertas)
    : IRequestHandler<ConfirmarRecorrencia, ResultadoLancamento>
{
    public Task<ResultadoLancamento> Handle(ConfirmarRecorrencia requisicao, CancellationToken cancellationToken) =>
        unidade.ExecutarIdempotenteAsync(requisicao.UsuarioId, requisicao.ChaveIdempotencia, requisicao, async () =>
        {
            var regra = await LeituraRecorrencias.ObterRegraAsync(unidade, requisicao.UsuarioId, requisicao.Id, requisicao.Competencia, cancellationToken);
            if (requisicao.Automatica && !await banco.Set<ConfiguracaoUsuario>().AsNoTracking().AnyAsync(c =>
                c.UsuarioId == requisicao.UsuarioId && c.ConfirmarAoVirarMes && c.ConfirmacaoAutomaticaDesde <= requisicao.Competencia, cancellationToken))
                throw new BusinessException("A confirmação automática foi desativada para este mês.");
            var excecao = await unidade.Ler<ExcecaoRecorrencia>(requisicao.UsuarioId)
                .SingleOrDefaultAsync(x => x.RegraRecorrenteId == regra.Id && x.Competencia == requisicao.Competencia, cancellationToken);
            if (excecao?.Ignorada == true || await unidade.Ler<Lancamento>(requisicao.UsuarioId)
                .AnyAsync(x => x.RegraRecorrenteId == regra.Id && x.Competencia == requisicao.Competencia, cancellationToken))
                throw new BusinessException("Esta ocorrência foi pulada ou já foi confirmada.");

            var previsto = PrevisaoRecorrencias.Projecao(regra, excecao, requisicao.Competencia, false);
            var tipo = regra.Tipo switch
            {
                TipoRecorrencia.ReceitaFixa => TipoLancamento.Receita,
                TipoRecorrencia.AporteFixo => TipoLancamento.AporteInvestimento,
                _ => TipoLancamento.Despesa
            };
            var dados = new DadosLancamento(tipo, regra.FormaPagamento, previsto.Valor, previsto.Data, regra.ContaId,
                regra.CategoriaId, regra.Descricao, requisicao.ChaveIdempotencia, regra.CartaoId, Competencia: previsto.Competencia,
                EfeitoEsperado: requisicao.EfeitoEsperado);
            var criados = await registro.CriarAsync(requisicao.UsuarioId, dados, regra.Id, cancellationToken);
            return new(criados.Select(x => x.Id).ToArray(), await alertas.AvaliarGravacaoAsync(requisicao.UsuarioId, criados, cancellationToken));
        }, cancellationToken);
}
