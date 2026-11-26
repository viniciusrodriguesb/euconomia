import { Pipe, PipeTransform } from '@angular/core';
import { tipos, formas, recorrencias } from '../../core/constants/rotulos';

@Pipe({ name: 'rotulo' })
export class RotuloPipe implements PipeTransform {
  transform(valor: string) { return [...tipos, ...formas, ...recorrencias].find(([id]) => id === valor)?.[1] ?? valor; }
}
