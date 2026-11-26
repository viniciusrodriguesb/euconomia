import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { expect, it, vi } from 'vitest';
import { exigirSessao } from './sessao.guard';
import { SessaoService } from './sessao.service';

it('libera a navegação autenticada e redireciona quando a sessão não pode ser renovada', async () => {
  const email = signal<string | null>('teste@example.test');
  const sessao = { email, verificar: vi.fn(), token: vi.fn(), limpar: () => email.set(null) };
  const destino = {};
  const createUrlTree = vi.fn().mockReturnValue(destino);
  TestBed.configureTestingModule({ providers: [
    { provide: SessaoService, useValue: sessao }, { provide: Router, useValue: { createUrlTree } }
  ] });
  const verificar = () => TestBed.runInInjectionContext(() => exigirSessao({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
  expect(await verificar()).toBe(true);
  expect(sessao.verificar).not.toHaveBeenCalled();
  sessao.email.set(null);
  sessao.verificar.mockResolvedValueOnce(true);
  expect(await verificar()).toBe(true);
  expect(sessao.token).toHaveBeenCalledOnce();
  sessao.verificar.mockResolvedValueOnce(false);
  expect(await verificar()).toBe(destino);
  sessao.verificar.mockRejectedValueOnce(new Error('Falhou'));
  expect(await verificar()).toBe(destino);
  expect(sessao.email()).toBeNull();
  expect(createUrlTree).toHaveBeenCalledWith(['/entrar']);
});
