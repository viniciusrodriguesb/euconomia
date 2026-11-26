import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Tela } from '../../core/ui/tela';
import { GuardarCodigoComponent } from '../../shared/guardar-codigo/guardar-codigo.component';
import { confirmarCampo } from '../../shared/validators/confirmar-campo';

@Component({
  imports: [ReactiveFormsModule, RouterLink, GuardarCodigoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cadastro-usuario.component.html'
})
export class CadastroUsuarioComponent extends Tela {
  private readonly router = inject(Router);
  readonly codigo = signal('');
  readonly contaCriada = signal(false);
  readonly sessao = this.sessaoService;
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(240)]],
    confirmacaoEmail: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(12), Validators.maxLength(128)]],
    confirmacaoSenha: ['', Validators.required]
  }, { validators: [confirmarCampo('email', 'confirmacaoEmail'), confirmarCampo('senha', 'confirmacaoSenha')] });

  enviar() {
    void this.executar(async () => {
      if (this.form.invalid) return;
      const dados = this.form.getRawValue();
      await this.sessaoService.token();
      await this.sessaoService.registrar(dados);
      this.contaCriada.set(true);
      this.form.reset();
      await this.sessaoService.entrar({ email: dados.email, senha: dados.senha });
      await this.sessaoService.token();
      await this.sessaoService.verificar();
      this.codigo.set((await this.sessaoService.gerarCodigo(dados.senha)).codigo);
    });
  }

  continuar() { void this.router.navigateByUrl('/'); }
}
