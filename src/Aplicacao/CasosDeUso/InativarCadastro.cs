using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record InativarCadastro(Guid UsuarioId, Guid Id, bool Conta) : IRequest;
