import type { ValidatorFn } from '@angular/forms';

export function confirmarCampo(campo: string, confirmacao: string): ValidatorFn {
  return controle => {
    const dados = controle.value as Record<string, string>;
    return dados[campo] === dados[confirmacao] ? null : { [confirmacao]: true };
  };
}
