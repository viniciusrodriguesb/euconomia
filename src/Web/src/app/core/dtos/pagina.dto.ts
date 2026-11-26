export interface Pagina<T> {
  itens: T[];
  paginaAtual: number;
  tamanho: number;
  total: number;
}
