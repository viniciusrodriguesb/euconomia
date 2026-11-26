import { expect, it } from 'vitest';
import { criarTela, estabilizar, painel, resumo } from '../../../../test/fixtures';
import { DashboardComponent } from './dashboard.component';

it('calcula agregados, projeções e totais sob competência e caixa', async () => {
  const { tela, fixture } = await criarTela(DashboardComponent, { dashboard: painel });
  expect(tela.agregado()).toMatchObject({ entradas: 10000n, livre: 6600n, cartao: 500n });
  expect(tela.percentual(3000n)).toBe('30%');
  expect(tela.composicao().fundo).toContain('conic-gradient');
  expect(tela.previstos()[0].variacao).toBe(-2000n);
  expect(tela.previstos()[1].realizado).toBe(0n);
  expect(tela.calendario()).toHaveLength(2);
  expect(tela.fluxo()[0].saidas).toBe(3200n);
  expect(tela.saldoNegativo('-0.01')).toBe(true);
  expect(tela.saidasDoMes(resumo)).toBe(3400n);
  tela.visao.set('Caixa');
  expect(tela.agregado().cartao).toBe(400n);
  expect(tela.saidasDoMes(resumo)).toBe(3800n);

  const itens = [1, 2, 3, 4];
  expect(tela.itensVisiveis('lista', itens)).toEqual([1, 2, 3]);
  tela.alternarLista('lista');
  expect(tela.itensVisiveis('lista', itens)).toBe(itens);
  tela.alternarLista('lista');
  expect(tela.expandido('lista')).toBe(false);

  tela.filtros.controls.inicio.setValue('');
  tela.buscar();
  await estabilizar(fixture);
  expect(tela.erro()).toBe('Selecione o mês inicial.');
  tela.painel.set(null);
  expect(tela.percentual(100n)).toBe('—');
  expect(tela.composicao().fundo).toBe('#e8eff8');
  expect(tela.calendario()).toEqual([]);
  expect(tela.fluxo()).toEqual([]);
  expect(tela.previstos()).toEqual([]);
});

it('consulta conferência e persiste preferências de alertas', async () => {
  const { tela, api, fixture } = await criarTela(DashboardComponent, {
    dashboard: painel, conferencia: { contas: 1, faturas: 0, compras: 0, divergencias: [] },
    'alertas/configuracao': { percentualGastoAmarelo: '70', percentualGastoVermelho: '90', percentualCustosFixos: '50',
      percentualComprometimento: '80', percentualLimiteCartao: '80', limiteTransacoesEstabelecimento: 10,
      multiplicadorDespesaAtipica: '3', alertasDesativados: 0 }
  });
  tela.configurar();
  await estabilizar(fixture);
  expect(tela.configurando()).toBe(true);
  tela.configurar();
  expect(tela.configurando()).toBe(false);
  tela.alternarAlerta(2);
  expect(tela.alertaAtivo(2)).toBe(false);
  tela.salvarConfiguracao();
  await estabilizar(fixture);
  expect(api.put).toHaveBeenCalledWith('alertas/configuracao', expect.objectContaining({ alertasDesativados: 4 }));
  tela.marcarLido('alerta');
  await estabilizar(fixture);
  expect(api.put).toHaveBeenCalledWith('alertas/alerta/lido');
  tela.conferir();
  await estabilizar(fixture);
  expect(tela.conferencia()?.contas).toBe(1);
});
