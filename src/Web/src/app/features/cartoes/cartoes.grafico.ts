import type { Compra } from '../../core/dtos/compra.dto';
import { inicioMes, somarMes } from '../../shared/utils/datas';
import { centavos } from '../../shared/utils/moeda';

const cores = ['#2158ca', '#19816c', '#7754b7', '#aa5a13', '#8f3a6c'];

export function montarGraficoCartoes(inicio: string, meses: number, compras: Compra[]) {
  const linhas = Array.from({ length: meses }, (_, i) => ({ mes: somarMes(inicio, i), valoresPorCartao: new Map<string, bigint>(), total: 0n }));
  const porMes = new Map(linhas.map(x => [x.mes, x]));
  const nomes = new Set<string>();
  for (const compra of compras) {
    for (const parcela of compra.parcelas.filter(p => !p.paga && !p.estornada)) {
      const linha = porMes.get(parcela.fatura.slice(0, 7));
      if (!linha) continue;
      const valor = centavos(parcela.valor);
      nomes.add(compra.cartao);
      linha.valoresPorCartao.set(compra.cartao, (linha.valoresPorCartao.get(compra.cartao) ?? 0n) + valor);
      linha.total += valor;
    }
  }
  const maior = linhas.reduce((a, b) => a > b.total ? a : b.total, 1n);
  const legenda = [...nomes].map((nome, i) => ({ nome, cor: cores[i % cores.length] }));
  return { legenda, linhas: linhas.map(linha => ({
    mes: inicioMes(linha.mes), total: linha.total, texto: `${linha.mes} · ${linha.valoresPorCartao.size} cartões`,
    partes: legenda.filter(x => linha.valoresPorCartao.has(x.nome)).map(x => {
      const valor = linha.valoresPorCartao.get(x.nome)!;
      return { ...x, valor, largura: Number(valor * 10000n / maior) / 100 };
    }),
  })) };
}
