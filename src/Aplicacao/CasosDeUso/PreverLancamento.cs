using MediatR;

namespace Aplicacao;

public sealed record PreverLancamento(Guid UsuarioId, DadosLancamento Dados) : IRequest<EfeitoLancamento>;
