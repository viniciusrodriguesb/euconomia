import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, Input, OnChanges, OnDestroy, ViewChild } from '@angular/core';
import { BrlPipe } from '../pipes/brl.pipe';
import type { EvolucaoGrafico } from './evolucao-grafico.dto';
import { MesBrPipe } from '../pipes/mes-br.pipe';
import { desenharEvolucao } from './desenhar-evolucao';
import { VerMaisComponent } from '../ver-mais/ver-mais.component';
import { LIMITE_PREVIA } from '../ver-mais/limite-previa';

@Component({
  selector: 'app-evolucao',
  imports: [BrlPipe, MesBrPipe, VerMaisComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './evolucao.component.html',
  styleUrl: './evolucao.component.css'
})
export class EvolucaoComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input({ required: true }) dados: EvolucaoGrafico[] = [];
  @Input() projecao = false;
  @ViewChild('grafico') grafico?: ElementRef<HTMLCanvasElement>;
  private observador?: ResizeObserver;
  expandido = false;

  get linhasVisiveis() { return this.expandido ? this.dados : this.dados.slice(-LIMITE_PREVIA); }

  ngAfterViewInit() {
    this.observador = new ResizeObserver(() => this.desenhar());
    if (this.grafico) this.observador.observe(this.grafico.nativeElement);
    this.desenhar();
  }
  ngOnChanges() {
    this.expandido = false;
    this.desenhar();
  }
  ngOnDestroy() { this.observador?.disconnect(); }
  temAjuste() { return this.dados.some(x => x.ajustado); }

  private desenhar() { desenharEvolucao(this.grafico?.nativeElement, this.dados); }
}
