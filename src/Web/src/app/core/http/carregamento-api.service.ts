import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CarregamentoApi {
  private pendentes = 0;
  private atraso?: ReturnType<typeof setTimeout>;
  private readonly exibir = signal(false);
  readonly visivel = this.exibir.asReadonly();

  iniciar() {
    this.pendentes++;
    if (this.pendentes !== 1) return;

    this.atraso = setTimeout(() => this.exibir.set(true), 140);
  }

  finalizar() {
    this.pendentes--;
    if (this.pendentes !== 0) return;

    clearTimeout(this.atraso);
    this.exibir.set(false);
  }
}
