import { expect, it } from 'vitest';
import { criarTela, estabilizar } from '../../../../test/fixtures';
import { RedefinirSenhaComponent } from './redefinir-senha.component';

it('confirma a nova senha e limpa os dados após uma redefinição', async () => {
  const { tela, fixture, sessao } = await criarTela(RedefinirSenhaComponent);
  tela.enviar();
  await estabilizar(fixture);
  expect(sessao.redefinirSenha).not.toHaveBeenCalled();
  const dados = { email: 'conta@example.test', codigo: 'ABC', senha: 'NovaSenha!2026', confirmacaoSenha: 'OutraSenha!2026' };
  tela.form.setValue(dados);
  tela.form.controls.confirmacaoSenha.markAsDirty();
  fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('As senhas devem ser iguais.');
  expect(tela.form.invalid).toBe(true);
  dados.confirmacaoSenha = dados.senha;
  tela.form.setValue(dados);
  tela.enviar();
  await estabilizar(fixture);
  expect(sessao.redefinirSenha).toHaveBeenCalledWith(dados);
  expect(sessao.limpar).toHaveBeenCalledOnce();
  expect(tela.form.getRawValue().codigo).toBe('');
  expect(fixture.nativeElement.textContent).toContain('Senha redefinida.');
});

it('mantém o formulário disponível após um código inválido', async () => {
  const { tela, fixture, sessao } = await criarTela(RedefinirSenhaComponent);
  tela.form.setValue({ email: 'conta@example.test', codigo: 'ABC', senha: 'NovaSenha!2026', confirmacaoSenha: 'NovaSenha!2026' });
  sessao.redefinirSenha.mockRejectedValueOnce(new Error('Confira os dados.'));
  tela.enviar();
  await estabilizar(fixture);
  expect(tela.concluido()).toBe(false);
  expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('Confira os dados.');
});
