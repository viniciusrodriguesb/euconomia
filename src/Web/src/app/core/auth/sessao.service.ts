import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Api } from '../http/api.service';
import type { Credenciais } from '../dtos/credenciais.dto';
import type { CadastroUsuario } from '../dtos/cadastro-usuario.dto';
import type { CodigoRecuperacao } from '../dtos/codigo-recuperacao.dto';
import type { RedefinirSenha } from '../dtos/redefinir-senha.dto';

@Injectable({ providedIn: 'root' })
export class SessaoService {
  private readonly api = inject(Api);
  private readonly emailAtivo = signal<string | null>(null);
  readonly email = this.emailAtivo.asReadonly();

  async verificar() {
    try {
      this.emailAtivo.set((await this.api.get<Pick<Credenciais, 'email'>>('sessao')).email);
      return true;
    } catch (erro) {
      if (erro instanceof HttpErrorResponse && erro.status === 401) {
        this.limpar();
        return false;
      }
      throw erro;
    }
  }
  entrar(credenciais: Credenciais) { return this.api.post('sessao/entrar', credenciais); }
  registrar(cadastro: CadastroUsuario) { return this.api.post('sessao/registrar', cadastro); }
  gerarCodigo(senha: string) { return this.api.post<CodigoRecuperacao>('recuperacao-conta/codigo', { senha }); }
  redefinirSenha(pedido: RedefinirSenha) { return this.api.post('recuperacao-conta/redefinir-senha', pedido); }
  async sair() {
    await this.api.post('sessao/sair');
    this.limpar();
  }
  limpar() { this.emailAtivo.set(null); }
  token() { return this.api.get<void>('sessao/token'); }
}
