export interface Fatura {
  id: string;
  cartaoId: string;
  cartao: string;
  competencia: string;
  dataFechamento: string;
  dataVencimento: string;
  total: string;
  status: string;
  lancamentoPagamentoId: string | null;
}
