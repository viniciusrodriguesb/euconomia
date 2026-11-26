import type { Resumo } from './resumo.dto';
import type { Projecao } from './projecao.dto';
import type { Ranking } from './ranking.dto';
import type { Fatura } from './fatura.dto';
import type { Ocorrencia } from './ocorrencia.dto';
import type { Alerta } from './alerta.dto';
import type { Evolucao } from './evolucao.dto';
import type { PatrimonioMensal } from './patrimonio-mensal.dto';
import type { ValorRealizado } from './valor-realizado.dto';

export interface Painel {
  periodo: Resumo[];
  projecao: Projecao[];
  saldoAtual: string;
  patrimonioInvestido: string;
  saldoProjetadoFimMes: string;
  mesAnterior: Resumo;
  mediaEntradas6Meses: string;
  mediaSaidas6Meses: string;
  categorias: Ranking[];
  estabelecimentos: Ranking[];
  faturas: Fatura[];
  previstos: Ocorrencia[];
  alertas: Alerta[];
  avisoPercentuais: string | null;
  patrimonioPorMes: PatrimonioMensal[];
  valoresRealizados: ValorRealizado[];
  evolucao: Evolucao[];
}
