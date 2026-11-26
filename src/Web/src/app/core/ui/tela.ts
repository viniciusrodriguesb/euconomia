import { HttpErrorResponse } from '@angular/common/http';
import { inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SessaoService } from '../auth/sessao.service';

export abstract class Tela {
  protected readonly sessaoService = inject(SessaoService);
  private readonly roteador = inject(Router);
  readonly erro = signal('');
  readonly aviso = signal('');
  readonly ocupado = signal(false);

  protected async executar(acao: () => Promise<void>, limparAviso = true) {
    if (this.ocupado()) return;
    this.ocupado.set(true);
    this.erro.set('');
    if (limparAviso) this.aviso.set('');
    try { await acao(); }
    catch (erro) {
      if (erro instanceof HttpErrorResponse && erro.status === 401 && this.sessaoService.email()) {
        this.sessaoService.limpar();
        void this.roteador.navigateByUrl('/entrar');
      }
      this.erro.set(this.mensagemDeErro(erro));
    } finally { this.ocupado.set(false); }
  }

  private mensagemDeErro(erro: unknown): string {
    if (erro instanceof HttpErrorResponse)
      return erro.error?.detail ?? 'Confira a conexão e tente novamente.';

    return erro instanceof Error ? erro.message : 'Não foi possível concluir. Tente novamente.';
  }
}
