import type { Evolucao } from '../../core/dtos/evolucao.dto';

export interface EvolucaoGrafico extends Omit<Evolucao, 'saidas'> {
  saidas: string | bigint;
  ajustado?: boolean;
}
