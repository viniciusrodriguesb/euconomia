import type { ParcelaPrevista } from './parcela-prevista.dto';

export interface Efeito {
  impactoNoSaldo: string;
  competencia: string;
  faturas: ParcelaPrevista[];
}
