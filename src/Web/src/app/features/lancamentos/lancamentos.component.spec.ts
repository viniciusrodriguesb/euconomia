import { expect, it, vi } from 'vitest';
import { cadastros, criarTela, estabilizar, movimento, pagina } from '../../../../test/fixtures';
import { LancamentosComponent } from './lancamentos.component';

const efeito = { impactoNoSaldo: '-20.15', competencia: movimento.competencia, faturas: [] };
const fontes = { cadastros, lancamentos: pagina([movimento], 20), 'lancamentos/lancamento': movimento };

it('valida conta, categoria e cartão e registra o efeito confirmado', async () => {
  const { tela, fixture, api } = await criarTela(LancamentosComponent, fontes);
  tela.salvar();
  await estabilizar(fixture);
  expect(api.post).not.toHaveBeenCalled();
  tela.prever();
  await estabilizar(fixture);
  expect(tela.erro()).toMatch(/obrigatórios/);

  tela.form.patchValue({ valor: '20,15', descricao: 'Compra' });
  tela.prever();
  await estabilizar(fixture);
  expect(tela.erro()).toBe('Selecione uma conta.');
  tela.form.patchValue({ contaId: 'conta' });
  tela.prever();
  await estabilizar(fixture);
  expect(tela.erro()).toBe('Selecione uma categoria.');
  tela.form.patchValue({ categoriaId: 'categoria', formaPagamento: 'CartaoCredito' });
  tela.prever();
  await estabilizar(fixture);
  expect(tela.erro()).toBe('Selecione um cartão.');
  tela.form.patchValue({ cartaoId: 'cartao', parcelas: 2, competencia: '2026-11', estabelecimentoId: 'loja' });
  api.post.mockResolvedValueOnce(efeito);
  tela.prever();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('lancamentos/previsao', expect.objectContaining({
    valor: '20.15', contaId: 'conta', cartaoId: 'cartao', parcelas: 2, competencia: '2026-11-01', estabelecimentoId: 'loja'
  }));
  tela.salvar();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('lancamentos', expect.objectContaining({ efeitoEsperado: efeito }));
  expect(tela.aviso()).toContain('Lançamento registrado.');
});

it('descarta previsão que voltou para um formulário que já mudou', async () => {
  const { tela, fixture, api } = await criarTela(LancamentosComponent, fontes);
  let resolver!: (resultado: unknown) => void;
  api.post.mockReturnValueOnce(new Promise(resolve => { resolver = resolve; }));
  tela.form.patchValue({ valor: '5', descricao: 'Saque', tipo: 'RetiradaSaldo', contaId: 'conta' });
  tela.prever();
  expect(api.post).toHaveBeenLastCalledWith('lancamentos/previsao', expect.objectContaining({ categoriaId: null }));
  tela.form.patchValue({ valor: '11' });
  resolver(efeito);
  await estabilizar(fixture);
  expect(tela.efeito()).toBeNull();
});

it('pagina, consulta o original e vincula estorno e correção com chaves estáveis', async () => {
  const { tela, fixture, api } = await criarTela(LancamentosComponent, fontes);
  (fixture.nativeElement.querySelector('#formulario') as HTMLElement).scrollIntoView = vi.fn();
  tela.filtrar('2026-11');
  await estabilizar(fixture);
  tela.paginar(2, 10);
  await estabilizar(fixture);
  expect(api.get).toHaveBeenLastCalledWith('lancamentos', { competencia: '2026-11-01', pagina: 2, tamanho: 10 });
  tela.abrirOriginal(movimento.id);
  await estabilizar(fixture);
  expect(tela.detalhe()).toBe(movimento);

  const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
  tela.estornar(movimento);
  expect(api.post).not.toHaveBeenCalled();
  confirmar.mockReturnValue(true);
  tela.estornar(movimento);
  await estabilizar(fixture);
  const pedidoOriginal = api.post.mock.calls[0][1];
  tela.corrigir(movimento);
  tela.cancelarCorrecao();
  expect(tela.corrigindo()).toBeNull();
  tela.corrigir(movimento);
  api.post.mockResolvedValueOnce(efeito);
  tela.prever();
  await estabilizar(fixture);
  tela.salvar();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenLastCalledWith('lancamentos/lancamento/estornar',
    expect.objectContaining({ ...pedidoOriginal as object, substituto: expect.objectContaining({ efeitoEsperado: efeito }) }));
  expect(tela.estornados().has(null)).toBe(true);
});
