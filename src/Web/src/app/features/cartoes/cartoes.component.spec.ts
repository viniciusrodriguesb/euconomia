import { expect, it } from 'vitest';
import { criarTela, estabilizar, fatura } from '../../../../test/fixtures';
import type { Compra } from '../../core/dtos/compra.dto';
import { CartoesComponent } from './cartoes.component';

it('agrega só parcelas em aberto no intervalo e escala as barras por valor', async () => {
  const aberta = { id: 'p', numero: 1, fatura: fatura.competencia, valor: '10', paga: false, estornada: false };
  const compra: Compra = {
    id: 'compra', cartaoId: fatura.cartaoId, cartao: fatura.cartao, compra: fatura.competencia, descricao: 'Compra',
    atual: 1, totalParcelas: 5, valorParcela: '10', faltaPagar: '20', terminaEm: fatura.competencia,
    parcelas: [aberta, { ...aberta, id: 'p2' }, { ...aberta, id: 'p3', paga: true },
      { ...aberta, id: 'p4', estornada: true }, { ...aberta, id: 'p5', fatura: '1900-01-01' }]
  };
  const { tela } = await criarTela(CartoesComponent, { faturas: [fatura], parcelas: [compra] });
  expect(tela.grafico().linhas[0]).toMatchObject({ total: 2000n, partes: [{ largura: 100 }] });
  expect(tela.grafico().linhas[1].partes).toEqual([]);
  expect(tela.podePagar(fatura)).toBe(true);
  expect(tela.podePagar({ ...fatura, status: 'Paga' })).toBe(false);
  expect(tela.podePagar({ ...fatura, dataFechamento: '2999-01-01' })).toBe(false);
  expect(tela.podePagar({ ...fatura, total: '0' })).toBe(false);
});

it('reutiliza a chave de idempotência após falha e limpa a fatura selecionada ao confirmar', async () => {
  const { tela, fixture, api } = await criarTela(CartoesComponent, { faturas: [fatura], parcelas: [] });
  tela.selecionar(fatura);
  expect(tela.selecionada()).toEqual(fatura);
  api.post.mockRejectedValueOnce(new Error('Falhou'));
  tela.pagar(fatura);
  await estabilizar(fixture);
  expect(tela.erro()).toBe('Falhou');
  tela.pagar(fatura);
  await estabilizar(fixture);
  expect(api.post.mock.calls[0]).toEqual(api.post.mock.calls[1]);
  expect(tela.aviso()).toContain('Fatura paga.');
  expect(tela.selecionada()).toBeNull();
});
