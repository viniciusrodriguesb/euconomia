using Dominio;

namespace Aplicacao;

public sealed record DadosRegra(TipoRecorrencia Tipo, decimal ValorBase, int DiaDoMes, DateOnly CompetenciaInicio,
    DateOnly? CompetenciaFim, FormaPagamento FormaPagamento, Guid ContaId, Guid CategoriaId, Guid? CartaoId, string Descricao);
