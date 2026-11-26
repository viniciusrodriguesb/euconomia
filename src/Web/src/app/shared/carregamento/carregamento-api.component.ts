import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CarregamentoApi } from '../../core/http/carregamento-api.service';

@Component({
  selector: 'app-carregamento-api',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './carregamento-api.component.html',
  styleUrl: './carregamento-api.component.css'
})
export class CarregamentoApiComponent {
  readonly carregamento = inject(CarregamentoApi);
}
