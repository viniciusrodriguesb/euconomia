export interface Ocorrencia {
  regraId: string;
  versaoRegra: number;
  versaoExcecao: number | null;
  competencia: string;
  data: string;
  tipo: string;
  descricao: string;
  valor: string;
  ajustada: boolean;
  ignorada: boolean;
  realizada: boolean;
  formaPagamento: string;
  contaId: string;
  cartaoId: string | null;
  categoriaId: string;
}
