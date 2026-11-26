import { Api } from '../../core/http/api.service';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BrlPipe } from '../../shared/pipes/brl.pipe';
import type { Cadastros } from '../../core/dtos/cadastros.dto';
import { DiaBrPipe } from '../../shared/pipes/dia-br.pipe';
import type { Efeito } from '../../core/dtos/efeito.dto';
import type { Lancamento } from '../../core/dtos/lancamento.dto';
import { MesBrPipe } from '../../shared/pipes/mes-br.pipe';
import type { Pagina } from '../../core/dtos/pagina.dto';
import type { Resultado } from '../../core/dtos/resultado.dto';
import { RotuloPipe } from '../../shared/pipes/rotulo.pipe';
import { Tela } from '../../core/ui/tela';
import { formas, tipos, vazio } from '../../core/constants/rotulos';
import { hoje, inicioMes } from '../../shared/utils/datas';
import { valorApi } from '../../shared/utils/moeda';
import { PaginacaoComponent } from '../../shared/paginacao/paginacao.component';

@Component({
  imports: [ReactiveFormsModule, BrlPipe, DiaBrPipe, MesBrPipe, RotuloPipe, RouterLink, PaginacaoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lancamentos.component.html',
  styleUrl: './lancamentos.component.css'
})
export class LancamentosComponent extends Tela {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder).nonNullable;
  readonly tiposLancamento = tipos.slice(0, 5);
  readonly pagamentos = formas;
  readonly dados = signal<Cadastros>(vazio);
  readonly extrato = signal<Pagina<Lancamento> | null>(null);
  readonly efeito = signal<Efeito | null>(null);
  readonly detalhe = signal<Lancamento | null>(null);
  readonly corrigindo = signal<Lancamento | null>(null);
  readonly mesFiltro = signal('');
  readonly estornados = computed(() => new Set(this.extrato()?.itens.map(x => x.lancamentoEstornadoId)));
  readonly credito = signal(false);
  readonly tipo = signal('Despesa');
  readonly form = this.fb.group({
    tipo: 'Despesa', formaPagamento: 'Pix', valor: ['', Validators.required], descricao: ['', Validators.required],
    dataLancamento: [hoje(), Validators.required], competencia: '', contaId: '', categoriaId: '', cartaoId: '', estabelecimentoId: '',
    parcelas: [1, [Validators.required, Validators.min(1), Validators.max(120)]],
  });
  private chave = crypto.randomUUID();
  private readonly estornos = new Map<string, string>();
  private dadosConfirmados: ReturnType<LancamentosComponent['montar']> | null = null;

  constructor() {
    super();
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      const dados = this.form.getRawValue();
      this.tipo.set(dados.tipo);
      this.credito.set(dados.formaPagamento === 'CartaoCredito');
      this.efeito.set(null);
      this.dadosConfirmados = null;
      this.chave = crypto.randomUUID();
    });
    void this.executar(async () => {
      const [dados, extrato] = await Promise.all([this.api.get<Cadastros>('cadastros'), this.buscarExtrato(1)]);
      this.dados.set(dados);
      this.extrato.set(extrato);
    });
  }

  private montar() {
    if (this.form.invalid) throw new Error('Preencha os campos obrigatórios.');
    const dados = this.form.getRawValue();
    const cartao = this.credito() ? this.dados().cartoes.find(x => x.id === dados.cartaoId) : undefined;
    if (this.credito() && !cartao) throw new Error('Selecione um cartão.');
    const contaId = cartao?.contaPagamentoId ?? dados.contaId;
    if (!contaId) throw new Error('Selecione uma conta.');
    if (this.tipo() !== 'RetiradaSaldo' && !dados.categoriaId) throw new Error('Selecione uma categoria.');
    return { ...dados, valor: valorApi(dados.valor), contaId, categoriaId: this.tipo() === 'RetiradaSaldo' ? null : dados.categoriaId,
      cartaoId: cartao?.id ?? null, competencia: dados.competencia ? inicioMes(dados.competencia) : null,
      estabelecimentoId: dados.estabelecimentoId || null, parcelas: cartao ? dados.parcelas : 1, chaveIdempotencia: this.chave };
  }
  prever() {
    void this.executar(async () => {
      const dados = this.montar();
      const efeito = await this.api.post<Efeito>('lancamentos/previsao', dados);
      if (dados.chaveIdempotencia !== this.chave) return;
      this.dadosConfirmados = dados;
      this.efeito.set(efeito);
    });
  }
  salvar() {
    void this.executar(async () => {
      const esperado = this.efeito();
      if (!this.dadosConfirmados || !esperado) return;
      const dados = { ...this.dadosConfirmados, efeitoEsperado: esperado };
      const correcao = this.corrigindo();
      const resposta = correcao
        ? await this.api.post<Resultado>(`lancamentos/${correcao.id}/estornar`, { data: hoje(), chaveIdempotencia: this.chaveEstorno(correcao.id), substituto: dados })
        : await this.api.post<Resultado>('lancamentos', dados);
      this.form.reset();
      this.corrigindo.set(null);
      this.aviso.set(['Lançamento registrado.', ...resposta.alertas].join(' '));
      this.extrato.set(await this.buscarExtrato(1));
    });
  }
  corrigir(item: Lancamento) {
    this.corrigindo.set(item);
    this.form.patchValue({
      tipo: item.tipo, formaPagamento: item.formaPagamento, valor: item.valor.replace('.', ','), descricao: item.descricao,
      dataLancamento: item.dataLancamento, competencia: item.competencia.slice(0, 7), contaId: item.contaId,
      categoriaId: item.categoriaId ?? '', cartaoId: item.cartaoId ?? '', estabelecimentoId: item.estabelecimentoId ?? '', parcelas: 1
    });
    document.getElementById('formulario')?.scrollIntoView({ block: 'start' });
  }
  cancelarCorrecao() { this.corrigindo.set(null); this.form.reset(); }
  abrirOriginal(id: string) {
    void this.executar(async () => this.detalhe.set(await this.api.get<Lancamento>(`lancamentos/${id}`)));
  }
  estornar(item: Lancamento) {
    if (!window.confirm(`Estornar “${item.descricao}”? Será gravada uma movimentação inversa vinculada ao original.`)) return;
    void this.executar(async () => {
      const resposta = await this.api.post<Resultado>(`lancamentos/${item.id}/estornar`, { data: hoje(), chaveIdempotencia: this.chaveEstorno(item.id) });
      this.aviso.set(['Estorno registrado.', ...resposta.alertas].join(' '));
      this.extrato.set(await this.buscarExtrato(1));
    });
  }
  filtrar(mes: string) { this.mesFiltro.set(mes); this.paginar(1); }
  paginar(pagina: number, tamanho?: number) {
    void this.executar(async () => this.extrato.set(await this.buscarExtrato(pagina, tamanho)));
  }
  private buscarExtrato(pagina: number, tamanho = this.extrato()?.tamanho ?? 20) {
    const filtro = this.mesFiltro();
    return this.api.get<Pagina<Lancamento>>('lancamentos', filtro ? { pagina, tamanho, competencia: inicioMes(filtro) } : { pagina, tamanho });
  }
  private chaveEstorno(id: string) {
    const chave = this.estornos.get(id) ?? crypto.randomUUID();
    this.estornos.set(id, chave);
    return chave;
  }
}
