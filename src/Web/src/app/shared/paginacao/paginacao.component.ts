import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import type { Pagina } from '../../core/dtos/pagina.dto';

@Component({
  selector: 'app-paginacao',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './paginacao.component.html',
  styleUrl: './paginacao.component.css'
})
export class PaginacaoComponent {
  readonly pagina = input<Pagina<unknown> | null>(null);
  readonly tamanhos = input.required<readonly number[]>();
  readonly tamanhoInicial = input.required<number>();
  readonly rotulo = input.required<string>();
  readonly ocupado = input(false);
  readonly paginaAlterada = output<number>();
  readonly tamanhoAlterado = output<number>();
  readonly tamanho = computed(() => this.pagina()?.tamanho ?? this.tamanhoInicial());
  readonly totalPaginas = computed(() => Math.max(1, Math.ceil((this.pagina()?.total ?? 0) / this.tamanho())));

  selecionarTamanho(seletor: HTMLSelectElement) {
    const tamanho = Number(seletor.value);
    seletor.value = String(this.tamanho());
    this.tamanhoAlterado.emit(tamanho);
  }
}
