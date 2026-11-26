import { TestBed } from '@angular/core/testing';
import { expect, it, vi } from 'vitest';
import { pagina } from '../../../test/fixtures';
import { CarregamentoApi } from '../core/http/carregamento-api.service';
import { CarregamentoApiComponent } from './carregamento/carregamento-api.component';
import { EvolucaoComponent } from './evolucao/evolucao.component';
import { PaginacaoComponent } from './paginacao/paginacao.component';
import { VerMaisComponent } from './ver-mais/ver-mais.component';

it('mostra o estado de carregamento com alternativa para movimento reduzido', () => {
  vi.useFakeTimers();
  const fixture = TestBed.createComponent(CarregamentoApiComponent);
  const carregamento = TestBed.inject(CarregamentoApi);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('picture')).toBeNull();
  carregamento.iniciar();
  vi.advanceTimersByTime(140);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('[role=status]').textContent).toContain('Carregando dados');
  expect(fixture.nativeElement.querySelector('source').getAttribute('srcset')).toContain('growth-estatico.png');
  carregamento.finalizar();
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('picture')).toBeNull();
});

it('pagina pelo total do servidor e emite intentos de navegação e tamanho', () => {
  const fixture = TestBed.createComponent(PaginacaoComponent);
  fixture.componentRef.setInput('rotulo', 'lançamentos');
  fixture.componentRef.setInput('tamanhoInicial', 20);
  fixture.componentRef.setInput('tamanhos', [10, 20]);
  fixture.detectChanges();
  const raiz = fixture.nativeElement as HTMLElement;
  const seletor = raiz.querySelector('select')!;
  expect(seletor.value).toBe('20');
  expect(fixture.componentInstance.totalPaginas()).toBe(1);
  fixture.componentRef.setInput('pagina', pagina([1], 10, 36, 2));
  fixture.detectChanges();
  const paginaAlterada = vi.fn();
  const tamanhoAlterado = vi.fn();
  fixture.componentInstance.paginaAlterada.subscribe(paginaAlterada);
  fixture.componentInstance.tamanhoAlterado.subscribe(tamanhoAlterado);
  expect(raiz.textContent).toContain('Página 2 de 4');
  expect(seletor.value).toBe('10');
  const [anterior, proxima] = raiz.querySelectorAll('button');
  anterior.click();
  proxima.click();
  expect(paginaAlterada.mock.calls).toEqual([[1], [3]]);
  seletor.value = '20';
  seletor.dispatchEvent(new Event('change'));
  expect(tamanhoAlterado).toHaveBeenCalledWith(20);
  expect(seletor.value).toBe('10');
});

it('oferece expansão somente acima de três resultados', () => {
  const fixture = TestBed.createComponent(VerMaisComponent);
  fixture.componentRef.setInput('rotulo', 'categorias');
  fixture.componentRef.setInput('total', 3);
  fixture.componentRef.setInput('expandido', false);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('button')).toBeNull();
  fixture.componentRef.setInput('total', 5);
  const alternar = vi.fn();
  fixture.componentInstance.alternar.subscribe(alternar);
  fixture.detectChanges();
  const botao = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
  expect(botao.textContent).toContain('Ver mais (2)');
  botao.click();
  expect(alternar).toHaveBeenCalledOnce();
  fixture.componentRef.setInput('expandido', true);
  fixture.detectChanges();
  expect(botao.textContent).toContain('Ver menos');
});

it('desenha barras e série de saldo, destaca ajustes e limita a tabela de meses', () => {
  const desenho = Object.fromEntries(['scale', 'beginPath', 'moveTo', 'lineTo', 'stroke', 'fillRect', 'fillText', 'arc', 'fill']
    .map(metodo => [metodo, vi.fn()]));
  vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(desenho as unknown as CanvasRenderingContext2D);
  vi.spyOn(HTMLCanvasElement.prototype, 'clientWidth', 'get').mockReturnValue(86);
  vi.spyOn(HTMLCanvasElement.prototype, 'clientHeight', 'get').mockReturnValue(120);
  vi.spyOn(window, 'devicePixelRatio', 'get').mockReturnValue(0);
  const fixture = TestBed.createComponent(EvolucaoComponent);
  fixture.componentRef.setInput('dados', []);
  fixture.detectChanges();
  expect(desenho['fillRect']).not.toHaveBeenCalled();
  const dados = [
    { mes: '2026-09-01', entradas: '100', saidas: '20', saldo: '-1' },
    { mes: '2026-10-01', entradas: '120', saidas: 500n, saldo: '25', ajustado: true },
    { mes: '2026-11-01', entradas: '171', saidas: '4', saldo: '50' },
    { mes: '2026-12-01', entradas: '173', saidas: '6', saldo: '30' }
  ];
  fixture.componentRef.setInput('dados', dados);
  fixture.detectChanges();
  expect(fixture.componentInstance.linhasVisiveis).toEqual(dados.slice(1));
  expect(fixture.componentInstance.temAjuste()).toBe(true);
  expect(desenho['fillRect']).toHaveBeenCalledTimes(8);
  expect(desenho['arc']).toHaveBeenCalledWith(expect.any(Number), expect.any(Number), 4.5, 0, Math.PI * 2);
  fixture.componentInstance.expandido = true;
  expect(fixture.componentInstance.linhasVisiveis).toBe(dados);
  fixture.destroy();
});
