import { expect, it } from 'vitest';
import { criarTela, estabilizar } from '../../../../test/fixtures';
import { CadastroUsuarioComponent } from './cadastro-usuario.component';

it('impede cadastro com confirmações diferentes e exibe o código antes de navegar', async () => {
  const { tela, fixture, sessao, navegar } = await criarTela(CadastroUsuarioComponent);
  tela.enviar();
  await estabilizar(fixture);
  expect(sessao.registrar).not.toHaveBeenCalled();
  const dados = { email: 'conta@example.test', confirmacaoEmail: 'outra@example.test',
    senha: 'MinhaSenha!2026', confirmacaoSenha: 'Diferente!2026' };
  tela.form.setValue(dados);
  tela.form.controls.confirmacaoEmail.markAsDirty();
  tela.form.controls.confirmacaoSenha.markAsDirty();
  fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Os e-mails devem ser iguais.');
  expect(fixture.nativeElement.textContent).toContain('As senhas devem ser iguais.');
  tela.enviar();
  await estabilizar(fixture);
  expect(sessao.registrar).not.toHaveBeenCalled();
  const cadastro = { ...dados, confirmacaoEmail: dados.email, confirmacaoSenha: dados.senha };
  tela.form.setValue(cadastro);
  tela.enviar();
  await estabilizar(fixture);
  expect(sessao.registrar).toHaveBeenCalledWith(cadastro);
  expect(sessao.entrar).toHaveBeenCalledWith({ email: dados.email, senha: dados.senha });
  expect(sessao.gerarCodigo).toHaveBeenCalledWith(dados.senha);
  expect(tela.codigo()).toBe('ABC123');
  expect(tela.form.getRawValue().senha).toBe('');
  expect(navegar).not.toHaveBeenCalled();
  tela.continuar();
  expect(navegar).toHaveBeenCalledWith('/');
});

it('apresenta falhas do servidor sem exibir um código', async () => {
  const { tela, fixture, sessao } = await criarTela(CadastroUsuarioComponent);
  tela.form.setValue({ email: 'conta@example.test', confirmacaoEmail: 'conta@example.test',
    senha: 'MinhaSenha!2026', confirmacaoSenha: 'MinhaSenha!2026' });
  sessao.registrar.mockRejectedValueOnce(new Error('Cadastro indisponível.'));
  tela.enviar();
  await estabilizar(fixture);
  expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('Cadastro indisponível.');
  expect(tela.codigo()).toBe('');
});

it('permite retomar a geração pela Segurança quando a conta foi criada e a geração falhou', async () => {
  const { tela, fixture, sessao } = await criarTela(CadastroUsuarioComponent);
  tela.form.setValue({ email: 'conta@example.test', confirmacaoEmail: 'conta@example.test',
    senha: 'MinhaSenha!2026', confirmacaoSenha: 'MinhaSenha!2026' });
  sessao.gerarCodigo.mockRejectedValueOnce(new Error('Tente gerar novamente.'));
  tela.enviar();
  await estabilizar(fixture);
  expect(tela.contaCriada()).toBe(true);
  expect(tela.form.getRawValue().senha).toBe('');
  expect(fixture.nativeElement.querySelector('form')).toBeNull();
  expect(fixture.nativeElement.textContent).toContain('Entrar na minha conta');
  sessao.email.set('conta@example.test');
  fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Abrir Segurança');
});
