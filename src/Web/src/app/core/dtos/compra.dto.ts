export interface Compra {
  id: string;
  cartaoId: string;
  cartao: string;
  compra: string;
  descricao: string;
  atual: number;
  totalParcelas: number;
  valorParcela: string;
  faltaPagar: string;
  terminaEm: string;
  parcelas: ParcelaCompra[];
}
import type { ParcelaCompra } from './parcela-compra.dto';

