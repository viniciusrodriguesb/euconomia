import { Pipe, PipeTransform } from '@angular/core';
const meses = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

@Pipe({ name: 'mesBr' })
export class MesBrPipe implements PipeTransform {
  transform(valor: string) { return `${meses[Number(valor.slice(5, 7)) - 1]}/${valor.slice(0, 4)}`; }
}
