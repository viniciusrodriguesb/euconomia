import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { LIMITE_PREVIA } from './limite-previa';

@Component({
  selector: 'app-ver-mais',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ver-mais.component.html',
  styleUrl: './ver-mais.component.css'
})
export class VerMaisComponent {
  readonly limite = LIMITE_PREVIA;
  readonly total = input.required<number>();
  readonly rotulo = input.required<string>();
  readonly expandido = input.required<boolean>();
  readonly alternar = output<void>();
}
