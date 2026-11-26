using MediatR;

namespace Aplicacao;

public sealed record ListarPrevisaoPaginada(Guid UsuarioId, DateOnly Inicio, int Meses, int Pagina, int Tamanho)
    : IRequest<Pagina<OcorrenciaPrevista>>;
