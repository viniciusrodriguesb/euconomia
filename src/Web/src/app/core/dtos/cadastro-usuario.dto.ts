import type { Credenciais } from './credenciais.dto';

export interface CadastroUsuario extends Credenciais {
  confirmacaoEmail: string;
  confirmacaoSenha: string;
}
