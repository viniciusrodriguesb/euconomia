using MediatR;

namespace Aplicacao;

public sealed record EstornarLancamento(Guid UsuarioId, Guid LancamentoId, DateOnly Data, Guid ChaveIdempotencia,
    DadosLancamento? Substituto = null) : IRequest<ResultadoLancamento>;
