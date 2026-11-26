import { Api } from '../../core/http/api.service';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BrlPipe } from '../../shared/pipes/brl.pipe';
import type { Cadastros } from '../../core/dtos/cadastros.dto';
import { DiaBrPipe } from '../../shared/pipes/dia-br.pipe';
import type { Efeito } from '../../core/dtos/efeito.dto';
import { MesBrPipe } from '../../shared/pipes/mes-br.pipe';
import type { Ocorrencia } from '../../core/dtos/ocorrencia.dto';
import type { Pagina } from '../../core/dtos/pagina.dto';
import type { Resultado } from '../../core/dtos/resultado.dto';
import { RotuloPipe } from '../../shared/pipes/rotulo.pipe';
import { Tela } from '../../core/ui/tela';
import { formas, recorrencias, vazio } from '../../core/constants/rotulos';
import { hoje, inicioMes, mesAtual } from '../../shared/utils/datas';
import { valorApi } from '../../shared/utils/moeda';
import { PaginacaoComponent } from '../../shared/paginacao/paginacao.component';

@Component({
  imports: [ReactiveFormsModule, RouterLink, BrlPipe, DiaBrPipe, MesBrPipe, RotuloPipe, PaginacaoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recorrencias.component.html',
  styleUrl: './recorrencias.component.css'
})
export class RecorrenciasComponent extends Tela {
  private readonly api = inject(Api);
  private readonly fb = inject(FormBuilder).nonNullable;
  readonly hojeIso = hoje();
  readonly tipos = recorrencias;
  readonly pagamentos = formas.slice(0, 5);
  readonly dados = signal<Cadastros>(vazio);
  readonly contas = computed(() => this.dados().contas.filter(x => !x.inativadoEm));
  readonly categorias = computed(() => this.dados().categorias.filter(x => !x.inativadoEm));
  readonly cartoes = computed(() => this.dados().cartoes.filter(x => !x.inativadoEm));
  readonly ocorrencias = signal<Pagina<Ocorrencia> | null>(null);
  readonly selecionada = signal<Ocorrencia | null>(null);
  readonly criando = signal(false);
  readonly assinatura = signal(false);
  readonly confirmacaoAutomatica = signal(false);
  readonly ajuste = this.fb.group({ modo: 'mes', valor: ['', Validators.required] });
  readonly filtros = this.fb.group({ inicio: [mesAtual(), Validators.required], meses: 6 });
  private periodoAplicado = this.filtros.getRawValue();
  readonly regra = this.fb.group({
    descricao: ['', Validators.required], tipo: 'CustoFixo', valorBase: ['', Validators.required], diaDoMes: [1, [Validators.min(1), Validators.max(31), Validators.required]],
    competenciaInicio: [mesAtual(), Validators.required], competenciaFim: '', formaPagamento: 'Pix',
    contaId: '', categoriaId: ['', Validators.required], cartaoId: '',
  });
  private readonly chaves = new Map<string, string>();

  constructor() {
    super();
    void this.executar(async () => {
      this.dados.set(await this.api.get<Cadastros>('cadastros'));
      this.confirmacaoAutomatica.set((await this.api.get<{ confirmarAoVirarMes: boolean }>('alertas/configuracao')).confirmarAoVirarMes);
      await this.carregar();
    });
  }
  trocarTipo() { this.assinatura.set(this.regra.controls.tipo.value === 'AssinaturaCartao'); }
  alternarAutomatico(ativa: boolean) {
    void this.executar(async () => {
      const anterior = this.confirmacaoAutomatica();
      this.confirmacaoAutomatica.set(ativa);
      try {
        await this.api.put('recorrencias/confirmacao-automatica', { ativa });
        this.aviso.set(ativa ? 'Confirmação automática ativada.' : 'Confirmação automática desativada.');
      } catch (erro) {
        this.confirmacaoAutomatica.set(anterior);
        throw erro;
      }
    });
  }
  buscar() {
    this.selecionada.set(null);
    void this.executar(() => this.carregar(1, undefined, this.filtros.getRawValue()));
  }
  paginar(pagina: number, tamanho?: number) {
    this.selecionada.set(null);
    void this.executar(() => this.carregar(pagina, tamanho));
  }
  private async carregar(pagina = this.ocorrencias()?.paginaAtual ?? 1, tamanho = this.ocorrencias()?.tamanho ?? 10,
    periodo = this.periodoAplicado) {
    const parametros = { inicio: inicioMes(periodo.inicio), meses: periodo.meses, tamanho };
    let resultado = await this.api.get<Pagina<Ocorrencia>>('recorrencias/previsao/paginada', { ...parametros, pagina });
    const ultimaPagina = Math.max(1, Math.ceil(resultado.total / tamanho));
    if (pagina > ultimaPagina)
      resultado = await this.api.get<Pagina<Ocorrencia>>('recorrencias/previsao/paginada', { ...parametros, pagina: ultimaPagina });

    this.periodoAplicado = periodo;
    this.ocorrencias.set(resultado);
  }
  registrar() {
    void this.executar(async () => {
      if (this.regra.invalid) return;
      const d = this.regra.getRawValue();
      const cartao = this.assinatura() ? this.cartoes().find(x => x.id === d.cartaoId) : undefined;
      if (this.assinatura() && !cartao) throw new Error('Selecione um cartão.');
      if (!cartao && !d.contaId) throw new Error('Selecione uma conta.');
      await this.api.post('recorrencias', { ...d, valorBase: valorApi(d.valorBase),
        competenciaInicio: inicioMes(d.competenciaInicio), competenciaFim: d.competenciaFim ? inicioMes(d.competenciaFim) : null,
        formaPagamento: cartao ? 'CartaoCredito' : d.formaPagamento, cartaoId: cartao?.id ?? null, contaId: cartao?.contaPagamentoId ?? d.contaId });
      this.regra.reset();
      this.trocarTipo();
      this.criando.set(false);
      this.aviso.set('Regra criada.');
      await this.carregar();
    });
  }
  editar(item: Ocorrencia) {
    this.selecionada.set(item);
    this.ajuste.reset({ modo: 'mes', valor: item.valor.replace('.', ',') });
  }
  salvarAjuste(item: Ocorrencia) {
    const dados = this.ajuste.getRawValue();
    if (dados.modo === 'adiante' && !window.confirm('Encerrar a versão atual no mês anterior e iniciar uma nova versão a partir deste mês?')) return;
    void this.executar(async () => {
      const valor = valorApi(dados.valor);
      if (dados.modo === 'adiante')
        await this.api.post(`recorrencias/${item.regraId}/versao`, { versao: item.versaoRegra, competencia: item.competencia, valor });
      else
        await this.enviarExcecao(item, valor, false, false);
      this.selecionada.set(null);
      this.aviso.set('Ajuste salvo.');
      await this.carregar();
    });
  }
  excecao(item: Ocorrencia, pular: boolean) {
    void this.executar(async () => {
      await this.enviarExcecao(item, null, pular, !pular);
      this.aviso.set(pular ? 'Mês pulado.' : 'Valor padrão restaurado.');
      await this.carregar();
    });
  }
  encerrar(item: Ocorrencia) {
    if (!window.confirm(`Encerrar “${item.descricao}” a partir de ${new MesBrPipe().transform(item.competencia)}? O mês selecionado e os seguintes saem da previsão desta regra.`)) return;
    void this.executar(async () => {
      await this.api.post(`recorrencias/${item.regraId}/encerrar`, { versao: item.versaoRegra, competencia: item.competencia });
      this.aviso.set('Regra encerrada.');
      this.selecionada.set(null);
      await this.carregar();
    });
  }
  private enviarExcecao(item: Ocorrencia, valor: string | null, ignorada: boolean, restaurar: boolean) {
    return this.api.put(`recorrencias/${item.regraId}/excecao`, {
      competencia: item.competencia, versaoRegra: item.versaoRegra, versaoExcecao: item.versaoExcecao, valor, ignorada, restaurar
    });
  }
  confirmar(item: Ocorrencia) {
    const id = item.regraId + item.competencia;
    const chaveIdempotencia = this.chaves.get(id) ?? crypto.randomUUID();
    this.chaves.set(id, chaveIdempotencia);
    void this.executar(async () => {
      const tipo = this.tipoDeLancamento(item.tipo);
      const efeitoEsperado = await this.api.post<Efeito>('lancamentos/previsao', {
        tipo, formaPagamento: item.formaPagamento, valor: item.valor, dataLancamento: item.data,
        categoriaId: item.categoriaId, contaId: item.contaId, cartaoId: item.cartaoId,
        competencia: item.competencia, descricao: item.descricao, chaveIdempotencia
      });
      const fatura = efeitoEsperado.faturas[0];
      const brl = new BrlPipe();
      const efeito = fatura
        ? `Entrada de ${brl.transform(fatura.valor)} na fatura de ${new MesBrPipe().transform(fatura.competencia)}, com vencimento em ${new DiaBrPipe().transform(fatura.vencimento)}.`
        : `Impacto no saldo de ${brl.transform(efeitoEsperado.impactoNoSaldo)} em ${new DiaBrPipe().transform(item.data)}.`;
      if (!window.confirm(`Confirmar “${item.descricao}” como realizado? ${efeito}`)) return;
      const resultado = await this.api.post<Resultado>(`recorrencias/${item.regraId}/confirmar`, {
        competencia: item.competencia, chaveIdempotencia, efeitoEsperado
      });
      this.aviso.set(['Ocorrência confirmada.', ...resultado.alertas].join(' '));
      await this.carregar();
    });
  }

  private tipoDeLancamento(tipo: string): string {
    if (tipo === 'AporteFixo') return 'AporteInvestimento';

    return tipo === 'ReceitaFixa' ? 'Receita' : 'Despesa';
  }
}
