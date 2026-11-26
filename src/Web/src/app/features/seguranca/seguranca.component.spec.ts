import { expect, it } from 'vitest';
import { criarTela, estabilizar } from '../../../../test/fixtures';
import { SegurancaComponent } from './seguranca.component';

it('exige a senha atual para gerar e fechar a exibição do código', async () => {
  const { tela, fixture, sessao } = await criarTela(SegurancaComponent);
  tela.gerar();
  await estabilizar(fixture);
  expect(sessao.gerarCodigo).not.toHaveBeenCalled();
  tela.form.setValue({ senha: 'SenhaAtual!2026' });
  tela.gerar();
  await estabilizar(fixture);
  expect(sessao.gerarCodigo).toHaveBeenCalledWith('SenhaAtual!2026');
  expect(tela.form.getRawValue().senha).toBe('');
  const checkbox = fixture.nativeElement.querySelector('input[type=checkbox]') as HTMLInputElement;
  checkbox.checked = true;
  checkbox.dispatchEvent(new Event('change'));
  fixture.detectChanges();
  fixture.nativeElement.querySelector('button').click();
  fixture.detectChanges();
  expect(tela.codigo()).toBe('');
  expect(fixture.nativeElement.textContent).toContain('Segurança da conta');
});

it('informa o erro de geração sem substituir o formulário', async () => {
  const { tela, fixture, sessao } = await criarTela(SegurancaComponent);
  tela.form.setValue({ senha: 'Incorreta' });
  sessao.gerarCodigo.mockRejectedValueOnce(new Error('Confira sua senha.'));
  tela.gerar();
  await estabilizar(fixture);
  expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('Confira sua senha.');
});
