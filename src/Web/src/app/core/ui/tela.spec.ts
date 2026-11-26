import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { SessaoService } from '../auth/sessao.service';
import { Tela } from './tela';

class TelaDeTeste extends Tela {
  executarAcao(acao: () => Promise<void>, limpar?: boolean) { return this.executar(acao, limpar); }
}

function prepararTela() {
  const email = signal<string | null>('teste@example.test');
  const navigateByUrl = vi.fn().mockResolvedValue(true);
  TestBed.configureTestingModule({ providers: [
    { provide: SessaoService, useValue: { email, limpar: () => email.set(null) } },
    { provide: Router, useValue: { navigateByUrl } }
  ] });
  return { tela: TestBed.runInInjectionContext(() => new TelaDeTeste()), email, navigateByUrl };
}

describe('estado de execução das telas', () => {
  it('evita concorrência e permite manter um aviso durante atualização', async () => {
    const { tela } = prepararTela();
    let resolver!: () => void;
    tela.aviso.set('Salvo.');
    const emCurso = tela.executarAcao(() => new Promise(resolve => { resolver = resolve; }), false);
    expect(tela.ocupado()).toBe(true);
    expect(tela.aviso()).toBe('Salvo.');
    const duplicada = vi.fn();
    await tela.executarAcao(duplicada);
    expect(duplicada).not.toHaveBeenCalled();
    resolver();
    await emCurso;
    expect(tela.ocupado()).toBe(false);
    await tela.executarAcao(async () => undefined);
    expect(tela.aviso()).toBe('');
  });

  it('apresenta o detalhe do servidor e volta ao login apenas quando havia sessão', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const { tela, email, navigateByUrl } = prepararTela();
    const falha = new HttpErrorResponse({ status: 401, error: { detail: 'Sessão expirada.' } });
    await tela.executarAcao(() => Promise.reject(falha));
    expect(tela.erro()).toBe('Sessão expirada.');
    expect(email()).toBeNull();
    expect(navigateByUrl).toHaveBeenCalledWith('/entrar');
    navigateByUrl.mockClear();
    await tela.executarAcao(() => Promise.reject(falha));
    expect(navigateByUrl).not.toHaveBeenCalled();
    await tela.executarAcao(() => Promise.reject(new HttpErrorResponse({ status: 503 })));
    expect(tela.erro()).toContain('conexão');
    await tela.executarAcao(() => Promise.reject(new Error('Falhou')));
    expect(tela.erro()).toBe('Falhou');
    await tela.executarAcao(() => Promise.reject(null));
    expect(tela.erro()).toContain('Não foi possível concluir');
  });
});
