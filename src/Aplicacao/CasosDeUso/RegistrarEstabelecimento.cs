using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record RegistrarEstabelecimento(Guid UsuarioId, DadosEstabelecimento Dados) : IRequest<Estabelecimento>;
