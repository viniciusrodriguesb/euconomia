import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Tela } from '../../core/ui/tela';

@Component({
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sessao.component.html'
})
export class SessaoComponent extends Tela {
  private readonly router = inject(Router);
  readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]], senha: ['', Validators.required]
  });
  enviar() {
    void this.executar(async () => {
      if (this.form.invalid) return;
      await this.sessaoService.token();
      const dados = this.form.getRawValue();
      await this.sessaoService.entrar(dados);
      await this.sessaoService.token();
      await this.sessaoService.verificar();
      await this.router.navigateByUrl('/');
    });
  }
}
