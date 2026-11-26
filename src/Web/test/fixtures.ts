import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Type, signal } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { Api } from '../src/app/core/http/api.service';
import { SessaoService } from '../src/app/core/auth/sessao.service';
import type { Cadastros } from '../src/app/core/dtos/cadastros.dto';
import type { Cartao } from '../src/app/core/dtos/cartao.dto';
import type { Fatura } from '../src/app/core/dtos/fatura.dto';
import type { Lancamento } from '../src/app/core/dtos/lancamento.dto';
import type { Ocorrencia } from '../src/app/core/dtos/ocorrencia.dto';
import type { Pagina } from '../src/app/core/dtos/pagina.dto';
import type { Resumo } from '../src/app/core/dtos/resumo.dto';
import type { Painel } from '../src/app/core/dtos/painel.dto';
import { hoje, inicioMes, mesAtual } from '../src/app/shared/utils/datas';

export const cartao: Cartao = {
  id: 'cartao', nome: 'Corporativo', bandeira: 'Visa', limite: '500', diaFechamento: 10, diaVencimento: 20,
  contaPagamentoId: 'conta', versao: 1, inativadoEm: null
};
export const cadastros: Cadastros = {
  contas: [{ id: 'conta', nome: 'Principal', saldoInicial: '100', dataSaldoInicial: hoje(), inativadoEm: null }],
  categorias: [{ id: 'categoria', nome: 'Alimentação', inativadoEm: null }],
  cartoes: [cartao],
  estabelecimentos: [{ id: 'loja', nome: 'Loja', limiteTransacoes: null }]
};
export const fatura: Fatura = {
  id: 'fatura', cartaoId: 'cartao', cartao: 'Corporativo', competencia: inicioMes(mesAtual()),
  dataFechamento: hoje(), dataVencimento: hoje(), total: '20.15', status: 'Fechada', lancamentoPagamentoId: null
};
export const movimento: Lancamento = {
  id: 'lancamento', tipo: 'Despesa', valor: '20.15', dataLancamento: hoje(), competencia: inicioMes(mesAtual()),
  descricao: 'Compra', formaPagamento: 'Pix', contaId: 'conta', categoriaId: 'categoria', cartaoId: null,
  estabelecimentoId: null, numeroParcela: null, totalParcelas: null, lancamentoEstornadoId: null
};
export const ocorrencia: Ocorrencia = {
  regraId: 'regra', versaoRegra: 1, versaoExcecao: null, competencia: inicioMes(mesAtual()), data: hoje(),
  tipo: 'CustoFixo', descricao: 'Aluguel', valor: '40', ajustada: false, ignorada: false, realizada: false,
  formaPagamento: 'Pix', contaId: 'conta', cartaoId: null, categoriaId: 'categoria'
};
export function pagina<T>(itens: T[], tamanho = 10, total = itens.length, paginaAtual = 1): Pagina<T> {
  return { itens, paginaAtual, tamanho, total };
}
export const resumo: Resumo = {
  mes: inicioMes(mesAtual()), entradas: '100', despesas: '30', fixos: '10', cartao: '5', aportes: '3',
  resgates: '2', retiradas: '1', pagamentos: '4', variacaoCaixa: '73', fixosCartao: '1'
};
export const painel: Painel = {
  periodo: [resumo], projecao: [{ mes: resumo.mes, entradas: '100', fixos: '20', parcelasEFaturas: '7', aportes: '5', saldoAoFim: '68', ajustado: true }],
  saldoAtual: '75', patrimonioInvestido: '3', saldoProjetadoFimMes: '68', mesAnterior: resumo, mediaEntradas6Meses: '99', mediaSaidas6Meses: '20',
  categorias: [{ nome: 'Alimentação', valor: '30', transacoes: 1 }], estabelecimentos: [],
  faturas: [fatura, { ...fatura, id: 'paga', status: 'Paga' }, { ...fatura, id: 'antiga', dataVencimento: '1900-01-01' }],
  previstos: [ocorrencia, { ...ocorrencia, regraId: 'feita', realizada: true }, { ...ocorrencia, regraId: 'ignorada', ignorada: true },
    { ...ocorrencia, regraId: 'aporte', tipo: 'AporteFixo' }],
  valoresRealizados: [{ regraId: 'regra', competencia: ocorrencia.competencia, valor: '20' }],
  alertas: [], avisoPercentuais: null, patrimonioPorMes: [], evolucao: []
};

export async function criarTela<T>(componente: Type<T>, respostas: Record<string, unknown> = {}) {
  const api = {
    get: vi.fn(async (rota: string, _parametros?: Record<string, unknown>) => respostas[rota]),
    post: vi.fn(async (_rota: string, _corpo?: unknown): Promise<unknown> => ({ alertas: [] })),
    put: vi.fn(async (_rota: string, _corpo?: unknown): Promise<unknown> => ({})),
    delete: vi.fn(async (_rota: string) => undefined)
  };
  const email = signal<string | null>(null);
  const limpar = vi.fn(() => email.set(null));
  const sessao = {
    email, verificar: vi.fn(async () => true), token: vi.fn(async () => undefined),
    entrar: vi.fn(async (_dados: unknown) => undefined), registrar: vi.fn(async (_dados: unknown) => undefined),
    gerarCodigo: vi.fn(async (_senha: string) => ({ codigo: 'ABC123' })),
    redefinirSenha: vi.fn(async (_dados: unknown) => undefined),
    sair: vi.fn(async () => { limpar(); }), limpar
  };
  await TestBed.configureTestingModule({
    imports: [componente],
    providers: [provideRouter([]), { provide: Api, useValue: api }, { provide: SessaoService, useValue: sessao }]
  }).compileComponents();
  const router = TestBed.inject(Router);
  const navegar = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  const fixture = TestBed.createComponent(componente);
  fixture.detectChanges();
  await estabilizar(fixture);
  return { tela: fixture.componentInstance, fixture, api, sessao, navegar };
}

export async function estabilizar<T>(fixture: ComponentFixture<T>) {
  await fixture.whenStable();
  const tela = fixture.componentInstance as { ocupado?: () => boolean };
  await vi.waitFor(() => {
    if (tela.ocupado?.()) throw new Error('A tela ainda está processando uma operação.');
  });
  fixture.detectChanges();
}
