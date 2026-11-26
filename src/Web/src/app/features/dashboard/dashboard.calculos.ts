import type { Painel } from '../../core/dtos/painel.dto';
import type { Projecao } from '../../core/dtos/projecao.dto';
import type { Resumo } from '../../core/dtos/resumo.dto';
import { hoje } from '../../shared/utils/datas';
import { centavos } from '../../shared/utils/moeda';

export function calcularAgregado(linhas: Resumo[], porCaixa: boolean) {
  const soma = (chave: Exclude<keyof Resumo, 'mes'>) => linhas.reduce((total, linha) => total + centavos(linha[chave]), 0n);
  const entradas = soma('entradas');
  const despesas = soma('despesas');
  const fixos = soma('fixos');
  const cartao = porCaixa ? soma('pagamentos') : soma('cartao');
  const aportes = soma('aportes');
  const retiradas = soma('retiradas');
  const resgates = soma('resgates');
  const fixosForaCartao = fixos - soma('fixosCartao');
  const variaveis = despesas - fixosForaCartao - soma('cartao');
  const saidas = despesas + (porCaixa ? cartao : 0n) + aportes + retiradas;
  const livre = entradas - saidas;
  return { entradas, despesas, fixos, cartao, aportes, retiradas, resgates, fixosForaCartao, variaveis, saidas, livre };
}

export function calcularComposicao(a: ReturnType<typeof calcularAgregado>) {
  const itens = [
    { nome: 'Fixos', valor: a.fixosForaCartao, cor: '#2354c5' }, { nome: 'Variáveis', valor: a.variaveis, cor: '#6294dc' },
    { nome: 'Cartão', valor: a.cartao, cor: '#7955b7' }, { nome: 'Aportes', valor: a.aportes, cor: '#18816a' },
    { nome: 'Retiradas', valor: a.retiradas, cor: '#ad621d' }
  ];
  const total = itens.reduce((s, item) => s + (item.valor > 0n ? item.valor : 0n), 0n);
  let acum = 0n;
  const pontos = itens.filter(x => x.valor > 0n).map(item => {
    const de = Number(acum * 10000n / total) / 100;
    acum += item.valor;
    const ate = Number(acum * 10000n / total) / 100;
    return `${item.cor} ${de}% ${ate}%`;
  });
  return { itens, fundo: pontos.length ? `conic-gradient(${pontos.join(', ')})` : '#e8eff8' };
}

export function compararOcorrencias(painel: Painel | null) {
  if (!painel) return [];
  const feitos = new Map(painel.valoresRealizados.map(x => [x.regraId + x.competencia, centavos(x.valor)]));
  return painel.previstos.map(item => {
    const realizado = feitos.get(item.regraId + item.competencia) ?? 0n;
    return { ...item, realizado, variacao: realizado - centavos(item.valor) };
  });
}

export function calcularFluxo(projecao: Projecao[]) {
  return projecao.map(mes => ({
  mes: mes.mes, entradas: mes.entradas, saldo: mes.saldoAoFim, ajustado: mes.ajustado,
  saidas: centavos(mes.fixos) + centavos(mes.parcelasEFaturas) + centavos(mes.aportes)
  }));
}

export function listarVencimentos(painel: Painel | null) {
  if (!painel) return [];
  const atual = hoje();
  return [
    ...painel.faturas.filter(x => x.status !== 'Paga').map(x => ({ chave: x.id, data: x.dataVencimento, descricao: `Fatura ${x.cartao}`, valor: x.total })),
    ...painel.previstos.filter(x => !x.realizada && !x.ignorada && x.tipo === 'CustoFixo')
      .map(x => ({ chave: x.regraId + x.competencia, data: x.data, descricao: x.descricao, valor: x.valor }))
  ].filter(x => x.data >= atual).sort((a, b) => a.data.localeCompare(b.data));
}
