using Dominio;

namespace Aplicacao;

public sealed record AlertaExibido(Guid? Id, TipoAlerta Tipo, GravidadeAlerta Gravidade, DateOnly Competencia,
    string Mensagem, string Referencia, DateTime? LidoEm);
