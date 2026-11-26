import { Api } from '../../core/http/api.service';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BrlPipe } from '../../shared/pipes/brl.pipe';
import type { Compra } from '../../core/dtos/compra.dto';
import { DiaBrPipe } from '../../shared/pipes/dia-br.pipe';
import type { Fatura } from '../../core/dtos/fatura.dto';
import { MesBrPipe } from '../../shared/pipes/mes-br.pipe';
import type { Resultado } from '../../core/dtos/resultado.dto';
import { Tela } from '../../core/ui/tela';
import { centavos } from '../../shared/utils/moeda';
import { hoje, inicioMes, mesAtual } from '../../shared/utils/datas';

import { montarGraficoCartoes } from './cartoes.grafico';

@Component({
  imports: [ReactiveFormsModule, RouterLink, BrlPipe, DiaBrPipe, MesBrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cartoes.component.html',
  styleUrl: './cartoes.component.css'
})
export class CartoesComponent extends Tela {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder).nonNullable;
  readonly filtro = this.fb.group({ inicio: [mesAtual(), Validators.required], meses: 6 });
  readonly pagamento = this.fb.group({ data: [hoje(), Validators.required] });
  readonly faturas = signal<Fatura[]>([]);
  readonly compras = signal<Compra[]>([]);
  readonly selecionada = signal<Fatura | null>(null);
  readonly periodo = signal(this.filtro.getRawValue());
  private readonly chaves = new Map<string, string>();
  readonly grafico = computed(() => {
    const { inicio, meses } = this.periodo();
    return montarGraficoCartoes(inicio, meses, this.compras());
  });

  constructor() { super(); this.buscar(); }
  buscar() { this.selecionada.set(null); void this.executar(() => this.carregar()); }
  private async carregar() {
    const { inicio, meses } = this.filtro.getRawValue();
    const [faturas, compras] = await Promise.all([
      this.api.get<Fatura[]>('faturas', { inicio: inicioMes(inicio), meses }),
      this.api.get<Compra[]>('parcelas', { mes: inicioMes(inicio) })
    ]);
    this.faturas.set(faturas);
    this.compras.set(compras);
    this.periodo.set({ inicio, meses });
  }
  podePagar(item: Fatura) { return item.status !== 'Paga' && item.dataFechamento <= hoje() && centavos(item.total) > 0n; }
  selecionar(item: Fatura) { this.pagamento.reset(); this.selecionada.set(item); }
  pagar(item: Fatura) {
    const data = this.pagamento.controls.data.value;
    const id = item.id + data;
    const chaveIdempotencia = this.chaves.get(id) ?? crypto.randomUUID();
    this.chaves.set(id, chaveIdempotencia);
    void this.executar(async () => {
      const resultado = await this.api.post<Resultado>(`faturas/${item.id}/pagar`, { data, chaveIdempotencia, valorEsperado: item.total });
      this.aviso.set(['Fatura paga.', ...resultado.alertas].join(' '));
      this.selecionada.set(null);
      this.chaves.delete(id);
      await this.carregar();
    });
  }
}
