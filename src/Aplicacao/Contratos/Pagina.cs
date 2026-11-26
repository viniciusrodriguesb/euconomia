namespace Aplicacao;

public sealed record Pagina<T>(T[] Itens, int PaginaAtual, int Tamanho, int Total);
