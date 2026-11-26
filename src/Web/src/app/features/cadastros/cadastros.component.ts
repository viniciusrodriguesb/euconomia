import { Api } from '../../core/http/api.service';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BrlPipe } from '../../shared/pipes/brl.pipe';
import type { Cadastros } from '../../core/dtos/cadastros.dto';
import type { Cartao } from '../../core/dtos/cartao.dto';
import { DiaBrPipe } from '../../shared/pipes/dia-br.pipe';
import { Tela } from '../../core/ui/tela';
import { hoje } from '../../shared/utils/datas';
import { valorApi } from '../../shared/utils/moeda';
import { vazio } from '../../core/constants/rotulos';

@Component({
  imports: [ReactiveFormsModule, BrlPipe, DiaBrPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cadastros.component.html'
})
export class CadastrosComponent extends Tela {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder).nonNullable;
  readonly dados = signal<Cadastros>(vazio);
  readonly contas = computed(() => this.dados().contas.filter(x => !x.inativadoEm));
  readonly conta = this.fb.group({ nome: ['', Validators.required], saldoInicial: ['0', Validators.required], dataSaldoInicial: [hoje(), Validators.required] });
  readonly categoria = this.fb.group({ nome: ['', Validators.required] });
  readonly loja = this.fb.group({ nome: ['', Validators.required], limiteTransacoes: this.fb.control<number | null>(null) });
  readonly acaoLoja = signal('alias');
  readonly gerenciarLoja = this.fb.group({
    acao: 'alias', id: ['', Validators.required], destinoId: '', nome: '', limiteTransacoes: this.fb.control<number | null>(null)
  });
  readonly cartao = this.fb.group({
    nome: ['', Validators.required], bandeira: ['', Validators.required], limite: ['', Validators.required], diaFechamento: [10, Validators.required],
    diaVencimento: [20, Validators.required], contaPagamentoId: ['', Validators.required], inativo: false
  });
  readonly cartaoId = signal<string | null>(null);
  private versao: number | null = null;

  constructor() { super(); void this.executar(() => this.carregar()); }
  private async carregar() { this.dados.set(await this.api.get<Cadastros>('cadastros')); }
  salvarConta() {
    void this.executar(async () => {
      if (this.conta.invalid) return;
      const dados = this.conta.getRawValue();
      await this.api.post('contas', { ...dados, saldoInicial: valorApi(dados.saldoInicial) });
      this.conta.reset();
      this.aviso.set('Conta adicionada.');
      await this.carregar();
    });
  }
  salvarCategoria() {
    void this.executar(async () => {
      if (this.categoria.invalid) return;
      await this.api.post('categorias', this.categoria.getRawValue());
      this.categoria.reset();
      this.aviso.set('Categoria adicionada.');
      await this.carregar();
    });
  }
  salvarLoja() {
    void this.executar(async () => {
      if (this.loja.invalid) return;
      await this.api.post('estabelecimentos', this.loja.getRawValue());
      this.loja.reset();
      this.aviso.set('Estabelecimento salvo.');
      await this.carregar();
    });
  }
  editarCartao(cartao: Cartao) {
    this.cartaoId.set(cartao.id);
    this.versao = cartao.versao;
    this.cartao.patchValue({ ...cartao, limite: cartao.limite.replace('.', ','), inativo: !!cartao.inativadoEm });
  }
  limparCartao() { this.cartaoId.set(null); this.versao = null; this.cartao.reset(); }
  salvarOrganizacao() {
    const d = this.gerenciarLoja.getRawValue();
    if (!d.id) return;
    if (d.acao === 'mesclar' && !window.confirm('Agrupar estes cadastros e o histórico associado no estabelecimento principal?')) return;
    void this.executar(async () => {
      const rota = `estabelecimentos/${d.id}`;
      if (d.acao === 'alias') await this.api.post(`${rota}/aliases`, { nome: d.nome });
      else if (d.acao === 'mesclar') {
        if (!d.destinoId) throw new Error('Selecione o estabelecimento principal.');
        await this.api.post(`${rota}/mesclar`, { destinoId: d.destinoId });
      } else await this.api.put(`${rota}/limite`, { limiteTransacoes: d.limiteTransacoes });
      this.gerenciarLoja.reset({ acao: d.acao });
      this.aviso.set('Organização salva.');
      await this.carregar();
    });
  }
  salvarCartao() {
    void this.executar(async () => {
      if (this.cartao.invalid) return;
      const dados = this.cartao.getRawValue();
      const body = { ...dados, limite: valorApi(dados.limite), versao: this.versao };
      if (this.cartaoId()) await this.api.put(`cartoes/${this.cartaoId()}`, body);
      else await this.api.post('cartoes', body);
      this.limparCartao();
      this.aviso.set('Cartão salvo.');
      await this.carregar();
    });
  }
  inativar(rota: string, id: string, nome: string) {
    if (!window.confirm(`Inativar ${nome}?`)) return;
    void this.executar(async () => { await this.api.delete(`${rota}/${id}`); await this.carregar(); });
  }
}
