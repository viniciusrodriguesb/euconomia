using MediatR;

namespace Aplicacao;

public sealed record RegistrarLancamento(Guid UsuarioId, DadosLancamento Dados) : IRequest<ResultadoLancamento>;
