import type { Conta } from './conta.dto';
import type { Categoria } from './categoria.dto';
import type { Cartao } from './cartao.dto';
import type { Estabelecimento } from './estabelecimento.dto';

export interface Cadastros {
  contas: Conta[];
  categorias: Categoria[];
  cartoes: Cartao[];
  estabelecimentos: Estabelecimento[];
}
