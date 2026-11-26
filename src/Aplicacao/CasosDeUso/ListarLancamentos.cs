using Dominio;
using MediatR;

namespace Aplicacao;

public sealed record ListarLancamentos(Guid UsuarioId, int Pagina, int Tamanho, DateOnly? Competencia) : IRequest<Pagina<Lancamento>>;
