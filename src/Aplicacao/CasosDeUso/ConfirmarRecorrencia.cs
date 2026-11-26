using MediatR;

namespace Aplicacao;

public sealed record ConfirmarRecorrencia(Guid UsuarioId, Guid Id, DateOnly Competencia, Guid ChaveIdempotencia,
    bool Automatica = false, EfeitoLancamento? EfeitoEsperado = null) : IRequest<ResultadoLancamento>;
