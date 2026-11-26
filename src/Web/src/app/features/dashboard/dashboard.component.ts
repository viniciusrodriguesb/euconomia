import { Api } from '../../core/http/api.service';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EvolucaoComponent } from '../../shared/evolucao/evolucao.component';
import { BrlPipe } from '../../shared/pipes/brl.pipe';
import { DiaBrPipe } from '../../shared/pipes/dia-br.pipe';
import { MesBrPipe } from '../../shared/pipes/mes-br.pipe';
import type { Painel } from '../../core/dtos/painel.dto';
import type { Resumo } from '../../core/dtos/resumo.dto';
import { Tela } from '../../core/ui/tela';
import { centavos, valorApi } from '../../shared/utils/moeda';
import { inicioMes, mesAtual } from '../../shared/utils/datas';
import { VerMaisComponent } from '../../shared/ver-mais/ver-mais.component';
import { LIMITE_PREVIA } from '../../shared/ver-mais/limite-previa';

import type { ConfiguracaoAlertas } from '../../core/dtos/configuracao-alertas.dto';
import type { Conferencia } from '../../core/dtos/conferencia.dto';
import { calcularAgregado, calcularComposicao, calcularFluxo, compararOcorrencias, listarVencimentos } from './dashboard.calculos';

@Component({
  imports: [ReactiveFormsModule, RouterLink, EvolucaoComponent, BrlPipe, DiaBrPipe, MesBrPipe, VerMaisComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent extends Tela {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder).nonNullable;
  readonly painel = signal<Painel | null>(null);
  readonly conferencia = signal<Conferencia | null>(null);
  readonly configurando = signal(false);
  private readonly listasExpandidas = signal<ReadonlySet<string>>(new Set());
  readonly alertasDesativados = signal(0);
  readonly tiposAlerta = ['Gasto alto', 'Custos fixos', 'Comprometimento futuro', 'Mesmo estabelecimento',
    'Despesa atípica', 'Limite do cartão', 'Saldo projetado negativo', 'Aporte abaixo do planejado'];
  readonly filtros = this.fb.group({ inicio: [mesAtual(), Validators.required], meses: 1, visao: 'Competencia', horizonte: 12 });
  readonly conf = this.fb.group({ amarelo: '', vermelho: '', fixos: '', comprometimento: '', cartao: '', estabelecimento: 10, atipica: '' });
  readonly visao = signal('Competencia');
  readonly agregado = computed(() => calcularAgregado(this.painel()?.periodo ?? [], this.visao() === 'Caixa'));
  readonly composicao = computed(() => calcularComposicao(this.agregado()));
  readonly previstos = computed(() => compararOcorrencias(this.painel()));
  readonly fluxo = computed(() => calcularFluxo(this.painel()?.projecao ?? []));
  readonly calendario = computed(() => listarVencimentos(this.painel()));

  constructor() { super(); this.buscar(); }
  buscar() {
    void this.executar(async () => {
      await this.carregar();
      this.listasExpandidas.set(new Set());
    });
  }
  expandido(lista: string) { return this.listasExpandidas().has(lista); }
  itensVisiveis<T>(lista: string, itens: T[]) { return this.expandido(lista) ? itens : itens.slice(0, LIMITE_PREVIA); }
  alternarLista(lista: string) {
    this.listasExpandidas.update(atual => {
      const proximo = new Set(atual);
      if (!proximo.delete(lista)) proximo.add(lista);
      return proximo;
    });
  }
  private async carregar() {
    const { inicio, meses, visao, horizonte } = this.filtros.getRawValue();
    if (!inicio) throw new Error('Selecione o mês inicial.');
    const resultado = await this.api.get<Painel>('dashboard', { inicio: inicioMes(inicio), meses, visao, horizonte });
    this.visao.set(visao);
    this.painel.set(resultado);
  }
  percentual(valor: bigint) {
    const base = this.agregado().entradas;
    if (base <= 0n) return '—';
    const taxa = Number(valor * 1000n / base) / 10;
    return `${taxa.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`;
  }
  saidasDoMes(mes: Resumo) {
    return centavos(mes.despesas) + centavos(mes.aportes) + centavos(mes.retiradas) +
      (this.visao() === 'Caixa' ? centavos(mes.pagamentos) : 0n);
  }
  saldoNegativo(valor: string) { return centavos(valor) < 0n; }
  configurar() {
    if (this.configurando()) { this.configurando.set(false); return; }
    void this.executar(async () => {
      const c = await this.api.get<ConfiguracaoAlertas>('alertas/configuracao');
      this.conf.reset({ amarelo: c.percentualGastoAmarelo, vermelho: c.percentualGastoVermelho, fixos: c.percentualCustosFixos,
        comprometimento: c.percentualComprometimento, cartao: c.percentualLimiteCartao,
        estabelecimento: c.limiteTransacoesEstabelecimento, atipica: c.multiplicadorDespesaAtipica });
      this.alertasDesativados.set(c.alertasDesativados);
      this.configurando.set(true);
    });
  }
  salvarConfiguracao() {
    void this.executar(async () => {
      const c = this.conf.getRawValue();
      await this.api.put('alertas/configuracao', { ...c, alertasDesativados: this.alertasDesativados(), amarelo: valorApi(c.amarelo), vermelho: valorApi(c.vermelho), fixos: valorApi(c.fixos),
        comprometimento: valorApi(c.comprometimento), cartao: valorApi(c.cartao), atipica: valorApi(c.atipica) });
      this.configurando.set(false);
      this.aviso.set('Limites salvos.');
      await this.carregar();
    });
  }
  alertaAtivo(indice: number) { return (this.alertasDesativados() & (1 << indice)) === 0; }
  alternarAlerta(indice: number) { this.alertasDesativados.update(a => a ^ (1 << indice)); }
  marcarLido(id: string) {
    void this.executar(async () => { await this.api.put(`alertas/${id}/lido`); await this.carregar(); });
  }
  conferir() {
    void this.executar(async () => this.conferencia.set(await this.api.get<Conferencia>('conferencia')));
  }
}
