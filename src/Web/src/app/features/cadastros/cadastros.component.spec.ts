import { expect, it, vi } from 'vitest';
import { cadastros, cartao, criarTela, estabilizar } from '../../../../test/fixtures';
import { CadastrosComponent } from './cadastros.component';

it('carrega cadastros e salva conta, categoria, loja e cartão com formulários válidos', async () => {
  const { tela, fixture, api } = await criarTela(CadastrosComponent, { cadastros });
  expect(tela.contas()).toEqual(cadastros.contas);
  for (const acao of [() => tela.salvarConta(), () => tela.salvarCategoria(), () => tela.salvarLoja(), () => tela.salvarCartao()]) {
    acao();
    await estabilizar(fixture);
  }
  expect(api.post).not.toHaveBeenCalled();

  tela.conta.patchValue({ nome: 'Dinheiro', saldoInicial: '50,20' });
  tela.salvarConta();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenCalledWith('contas', expect.objectContaining({ saldoInicial: '50.20' }));

  tela.categoria.patchValue({ nome: 'Casa' });
  tela.salvarCategoria();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenCalledWith('categorias', { nome: 'Casa' });
  tela.loja.patchValue({ nome: 'Mercado', limiteTransacoes: 8 });
  tela.salvarLoja();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenCalledWith('estabelecimentos', { nome: 'Mercado', limiteTransacoes: 8 });

  tela.cartao.patchValue(cartao);
  tela.salvarCartao();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenCalledWith('cartoes', expect.objectContaining({ versao: null }));
  tela.editarCartao({ ...cartao, inativadoEm: '2026-08-02' });
  tela.salvarCartao();
  await estabilizar(fixture);
  expect(api.put).toHaveBeenCalledWith('cartoes/cartao', expect.objectContaining({ versao: 1, inativo: true }));
  expect(tela.cartaoId()).toBeNull();
  tela.dados.set({ ...cadastros, contas: [{ ...cadastros.contas[0], inativadoEm: '2026-01-01' }] });
  expect(tela.contas()).toEqual([]);
});

it('organiza estabelecimentos e exige confirmação para ações destrutivas', async () => {
  const { tela, fixture, api } = await criarTela(CadastrosComponent, { cadastros });
  const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
  tela.salvarOrganizacao();
  tela.inativar('contas', 'conta', 'Principal');
  expect(api.delete).not.toHaveBeenCalled();
  tela.gerenciarLoja.patchValue({ id: 'loja', acao: 'mesclar' });
  tela.salvarOrganizacao();
  expect(api.post).not.toHaveBeenCalled();

  confirmar.mockReturnValue(true);
  tela.salvarOrganizacao();
  await estabilizar(fixture);
  expect(tela.erro()).toContain('estabelecimento principal');
  tela.gerenciarLoja.patchValue({ destinoId: 'destino' });
  tela.salvarOrganizacao();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenCalledWith('estabelecimentos/loja/mesclar', { destinoId: 'destino' });

  tela.gerenciarLoja.patchValue({ id: 'loja', acao: 'alias', nome: 'Apelido' });
  tela.salvarOrganizacao();
  await estabilizar(fixture);
  expect(api.post).toHaveBeenCalledWith('estabelecimentos/loja/aliases', { nome: 'Apelido' });
  tela.gerenciarLoja.patchValue({ id: 'loja', acao: 'limite', limiteTransacoes: 4 });
  tela.salvarOrganizacao();
  await estabilizar(fixture);
  expect(api.put).toHaveBeenCalledWith('estabelecimentos/loja/limite', { limiteTransacoes: 4 });

  tela.inativar('contas', 'conta', 'Principal');
  await estabilizar(fixture);
  expect(api.delete).toHaveBeenCalledWith('contas/conta');
});
