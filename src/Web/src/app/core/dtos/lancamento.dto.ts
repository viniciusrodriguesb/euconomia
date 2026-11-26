export interface Lancamento {
  id: string;
  tipo: string;
  valor: string;
  dataLancamento: string;
  competencia: string;
  descricao: string;
  formaPagamento: string;
  contaId: string;
  categoriaId: string | null;
  cartaoId: string | null;
  estabelecimentoId: string | null;
  numeroParcela: number | null;
  totalParcelas: number | null;
  lancamentoEstornadoId: string | null;
}
