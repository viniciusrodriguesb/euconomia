import { TestBed } from '@angular/core/testing';
import { expect, it, vi } from 'vitest';
import { GuardarCodigoComponent } from './guardar-codigo.component';

it('exibe o código sem persistir e permite continuar após confirmar que foi guardado', () => {
  const fixture = TestBed.createComponent(GuardarCodigoComponent);
  fixture.componentRef.setInput('codigo', 'ABC123');
  const concluir = vi.fn();
  fixture.componentInstance.concluir.subscribe(concluir);
  fixture.detectChanges();
  const botao = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
  expect(botao.disabled).toBe(true);
  expect(fixture.nativeElement.querySelector('textarea').value).toBe('ABC123');
  const checkbox = fixture.nativeElement.querySelector('input') as HTMLInputElement;
  checkbox.checked = true;
  checkbox.dispatchEvent(new Event('change'));
  fixture.detectChanges();
  botao.click();
  expect(concluir).toHaveBeenCalledOnce();
});
