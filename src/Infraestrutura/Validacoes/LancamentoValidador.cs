using Aplicacao;
using Dominio;

namespace Infraestrutura;

internal static class LancamentoValidador
{
    public static async Task<Cartao?> ValidarAsync(UnidadeFinanceira unidade, Guid usuarioId, DadosLancamento dados, CancellationToken cancellationToken)
    {
        ValidarCampos(dados);

        var conta = await unidade.ObterAsync<Conta>(usuarioId, dados.ContaId, "Conta não encontrada. Selecione outra conta.", cancellationToken);
        if (conta.InativadoEm.HasValue || dados.DataLancamento < conta.DataSaldoInicial)
            throw new BusinessException("Selecione uma conta ativa e informe uma data a partir do saldo inicial.");

        await ValidarCategoriaAsync(unidade, usuarioId, dados, cancellationToken);
        if (dados.EstabelecimentoId is { } estabelecimentoId)
            await unidade.ObterAsync<Estabelecimento>(usuarioId, estabelecimentoId, "Estabelecimento não encontrado.", cancellationToken);

        return await ValidarCartaoAsync(unidade, usuarioId, conta.Id, dados, cancellationToken);
    }

    public static void ConferirEfeito(EfeitoLancamento? esperado, DateOnly competencia, decimal saldo, ParcelaPrevista[] faturas)
    {
        if (esperado is not null && (esperado.ImpactoNoSaldo != saldo || esperado.Competencia != competencia ||
            esperado.Faturas is null || !esperado.Faturas.SequenceEqual(faturas)))
            throw new ConcorrenciaException();
    }

    public static void ValidarFaturaAbertaParaCompra(Fatura fatura)
    {
        if (fatura.Status == StatusFatura.Paga)
            throw new BusinessException("A compra pertence a uma fatura paga. Confira a data da compra.");
    }

    private static void ValidarCampos(DadosLancamento dados)
    {
        ValorMonetarioValidador.Validar(dados.Valor);

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        if (dados.DataLancamento == default || dados.DataLancamento > hoje || dados.DataLancamento.Year > 9988 ||
            dados.Competencia is { } competencia && (competencia.Day != 1 || competencia.Year > 9988))
            throw new BusinessException("Informe a data até hoje e a competência como o primeiro dia do mês.");

        if (string.IsNullOrWhiteSpace(dados.Descricao) || dados.Descricao.Length > 240 || dados.ChaveIdempotencia == Guid.Empty)
            throw new BusinessException("Informe uma descrição de até 240 caracteres e gere uma chave de idempotência.");

        if (dados.Tipo is < TipoLancamento.Receita or > TipoLancamento.RetiradaSaldo || !Enum.IsDefined(dados.FormaPagamento))
            throw new BusinessException("Selecione um tipo de lançamento e uma forma de pagamento válidos.");
    }

    private static async Task ValidarCategoriaAsync(UnidadeFinanceira unidade, Guid usuarioId, DadosLancamento dados,
        CancellationToken cancellationToken)
    {
        if (dados.CategoriaId is { } categoria)
        {
            var registro = await unidade.ObterAsync<Categoria>(usuarioId, categoria, "Categoria não encontrada.", cancellationToken);
            if (registro.InativadoEm.HasValue)
                throw new BusinessException("Selecione uma categoria ativa.");
        }
        else if (dados.Tipo != TipoLancamento.RetiradaSaldo)
            throw new BusinessException("Selecione uma categoria.");

        if (dados.Tipo == TipoLancamento.RetiradaSaldo && dados.CategoriaId is not null)
            throw new BusinessException("Retirada de saldo deve ser registrada sem categoria.");
    }

    private static async Task<Cartao?> ValidarCartaoAsync(UnidadeFinanceira unidade, Guid usuarioId, Guid contaId,
        DadosLancamento dados, CancellationToken cancellationToken)
    {
        if (dados.FormaPagamento != FormaPagamento.CartaoCredito)
        {
            if (dados.CartaoId is not null || dados.Parcelas != 1)
                throw new BusinessException("Selecione crédito para informar cartão ou parcelamento.");
            return null;
        }

        if (dados.Tipo != TipoLancamento.Despesa || dados.CartaoId is not { } id)
            throw new BusinessException("Selecione uma despesa e um cartão para lançar no crédito.");
        var cartao = await unidade.ObterAsync<Cartao>(usuarioId, id, "Cartão não encontrado.", cancellationToken);
        if (cartao.InativadoEm.HasValue || cartao.ContaPagamentoId != contaId)
            throw new BusinessException("Selecione um cartão ativo e a conta de pagamento desse cartão.");
        return cartao;
    }
}
