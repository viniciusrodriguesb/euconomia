using Dominio;

namespace Aplicacao;

public sealed record DadosLancamento(TipoLancamento Tipo, FormaPagamento FormaPagamento, decimal Valor,
    DateOnly DataLancamento, Guid ContaId, Guid? CategoriaId, string Descricao, Guid ChaveIdempotencia,
    Guid? CartaoId = null, Guid? EstabelecimentoId = null, DateOnly? Competencia = null, int Parcelas = 1,
    EfeitoLancamento? EfeitoEsperado = null);
