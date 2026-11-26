import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Tela } from '../../core/ui/tela';
import { GuardarCodigoComponent } from '../../shared/guardar-codigo/guardar-codigo.component';

@Component({
  imports: [ReactiveFormsModule, GuardarCodigoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './seguranca.component.html'
})
export class SegurancaComponent extends Tela {
  readonly codigo = signal('');
  readonly form = inject(FormBuilder).nonNullable.group({
    senha: ['', [Validators.required, Validators.maxLength(128)]]
  });

  gerar() {
    void this.executar(async () => {
      if (this.form.invalid) return;
      await this.sessaoService.token();
      this.codigo.set((await this.sessaoService.gerarCodigo(this.form.getRawValue().senha)).codigo);
      this.form.reset();
    });
  }
}
