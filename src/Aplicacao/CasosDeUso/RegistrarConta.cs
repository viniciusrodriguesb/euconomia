using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record RegistrarConta(Guid UsuarioId, DadosConta Dados) : IRequest<Conta>;
