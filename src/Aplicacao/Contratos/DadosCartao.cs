namespace Aplicacao;

public sealed record DadosCartao(string Nome, string Bandeira, decimal Limite, int DiaFechamento, int DiaVencimento,
    Guid ContaPagamentoId, uint? Versao = null, bool Inativo = false);
