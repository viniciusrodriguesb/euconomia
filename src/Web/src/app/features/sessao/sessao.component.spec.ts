import { expect, it } from 'vitest';
import { criarTela, estabilizar } from '../../../../test/fixtures';
import { SessaoComponent } from './sessao.component';

it('valida o formulário antes do login e inicia a sessão', async () => {
  const { tela, fixture, sessao, navegar } = await criarTela(SessaoComponent);
  tela.enviar();
  await estabilizar(fixture);
  expect(sessao.token).not.toHaveBeenCalled();
  const dados = { email: 'conta@example.test', senha: 'Teste!2026' };
  tela.form.setValue(dados);
  tela.enviar();
  await estabilizar(fixture);
  expect(sessao.registrar).not.toHaveBeenCalled();
  expect(sessao.entrar).toHaveBeenCalledWith(dados);
  expect(navegar).toHaveBeenCalledWith('/');

  expect(sessao.token).toHaveBeenCalledTimes(2);
  expect(sessao.verificar).toHaveBeenCalledOnce();
});
