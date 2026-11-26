namespace Dominio;

public sealed class ConfiguracaoUsuario
{
    public Guid UsuarioId { get; set; }
    public decimal PercentualGastoAmarelo { get; set; } = 70;
    public decimal PercentualGastoVermelho { get; set; } = 90;
    public decimal PercentualCustosFixos { get; set; } = 50;
    public decimal PercentualComprometimento { get; set; } = 80;
    public decimal PercentualLimiteCartao { get; set; } = 80;
    public int LimiteTransacoesEstabelecimento { get; set; } = 10;
    public decimal MultiplicadorDespesaAtipica { get; set; } = 3;
    public bool ConfirmarAoVirarMes { get; set; }
    public DateOnly? ConfirmacaoAutomaticaDesde { get; set; }
    public int AlertasDesativados { get; set; }
}
