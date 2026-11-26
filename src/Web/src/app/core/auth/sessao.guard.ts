import { inject } from '@angular/core';
import { CanActivateChildFn, Router } from '@angular/router';
import { SessaoService } from './sessao.service';

export const exigirSessao: CanActivateChildFn = async () => {
  const sessao = inject(SessaoService);
  const router = inject(Router);
  if (sessao.email()) return true;
  try {
    if (await sessao.verificar()) {
      await sessao.token();
      return true;
    }
  } catch {
    sessao.limpar();
  }
  return router.createUrlTree(['/entrar']);
};
