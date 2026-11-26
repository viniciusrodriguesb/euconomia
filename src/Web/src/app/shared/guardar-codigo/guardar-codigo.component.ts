import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

@Component({
  selector: 'app-guardar-codigo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './guardar-codigo.component.html'
})
export class GuardarCodigoComponent {
  readonly codigo = input.required<string>();
  readonly concluir = output<void>();
  readonly salvo = signal(false);
}
