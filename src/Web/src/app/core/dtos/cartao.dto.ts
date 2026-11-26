export interface Cartao {
  id: string;
  nome: string;
  bandeira: string;
  limite: string;
  diaFechamento: number;
  diaVencimento: number;
  contaPagamentoId: string;
  versao: number;
  inativadoEm: string | null;
}
