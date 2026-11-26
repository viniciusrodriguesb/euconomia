import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'diaBr' })
export class DiaBrPipe implements PipeTransform {
  transform(valor: string) { return valor.slice(0, 10).split('-').reverse().join('/'); }
}
