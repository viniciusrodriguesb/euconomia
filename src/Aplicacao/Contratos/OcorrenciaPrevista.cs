using Dominio;

namespace Aplicacao;

public sealed record OcorrenciaPrevista(Guid RegraId, uint VersaoRegra, uint? VersaoExcecao, DateOnly Competencia,
    DateOnly Data, TipoRecorrencia Tipo, string Descricao, decimal Valor, bool Ajustada, bool Ignorada, bool Realizada,
    FormaPagamento FormaPagamento, Guid ContaId, Guid? CartaoId, Guid CategoriaId);
