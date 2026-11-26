import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Tela } from '../../core/ui/tela';
import { confirmarCampo } from '../../shared/validators/confirmar-campo';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './redefinir-senha.component.html'
})
export class RedefinirSenhaComponent extends Tela {
  readonly concluido = signal(false);
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(240)]],
    codigo: ['', [Validators.required, Validators.maxLength(100)]],
    senha: ['', [Validators.required, Validators.minLength(12), Validators.maxLength(128)]],
    confirmacaoSenha: ['', Validators.required]
  }, { validators: confirmarCampo('senha', 'confirmacaoSenha') });

  enviar() {
    void this.executar(async () => {
      if (this.form.invalid) return;
      await this.sessaoService.token();
      await this.sessaoService.redefinirSenha(this.form.getRawValue());
      this.sessaoService.limpar();
      this.form.reset();
      this.concluido.set(true);
    });
  }
}
