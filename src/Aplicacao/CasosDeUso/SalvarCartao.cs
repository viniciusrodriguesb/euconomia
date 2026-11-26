using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record SalvarCartao(Guid UsuarioId, Guid? Id, DadosCartao Dados) : IRequest<Cartao>;
