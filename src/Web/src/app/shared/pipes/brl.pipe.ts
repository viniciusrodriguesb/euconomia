import { Pipe, PipeTransform } from '@angular/core';
import { centavos } from '../utils/moeda';

const inteiroBR = new Intl.NumberFormat('pt-BR');

@Pipe({ name: 'brl' })
export class BrlPipe implements PipeTransform {
  transform(valor: string | bigint | null | undefined) {
    if (valor == null) return '—';
    const v = typeof valor === 'bigint' ? valor : centavos(valor);
    const abs = v < 0n ? -v : v;
    return `${v < 0n ? '−' : ''}R$ ${inteiroBR.format(abs / 100n)},${String(abs % 100n).padStart(2, '0')}`;
  }
}
