import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { SessaoService } from '../auth/sessao.service';
import { Api } from './api.service';
import { CarregamentoApi } from './carregamento-api.service';
import { carregamentoApiInterceptor } from './carregamento-api.interceptor';

function prepararHttp() {
  TestBed.configureTestingModule({ providers: [
    provideHttpClient(withInterceptors([carregamentoApiInterceptor])), provideHttpClientTesting()
  ] });
  return {
    http: TestBed.inject(HttpClient), api: TestBed.inject(Api), sessao: TestBed.inject(SessaoService),
    rede: TestBed.inject(HttpTestingController), carregamento: TestBed.inject(CarregamentoApi)
  };
}

describe('acesso HTTP', () => {
  it('envia métodos, parâmetros e corpos esperados', async () => {
    const { api, rede } = prepararHttp();
    const leitura = api.get('lancamentos', { pagina: 2 });
    rede.expectOne({ method: 'GET', url: '/api/lancamentos?pagina=2' }).flush([{ id: 10 }]);
    await expect(leitura).resolves.toEqual([{ id: 10 }]);

    const gravacao = api.post('cadastros', { nome: 'Nova conta' });
    const post = rede.expectOne({ method: 'POST', url: '/api/cadastros' });
    expect(post.request.body).toEqual({ nome: 'Nova conta' });
    post.flush(null);
    await gravacao;

    const atualizacao = api.put('contas/10');
    const put = rede.expectOne({ method: 'PUT', url: '/api/contas/10' });
    expect(put.request.body).toEqual({});
    put.flush(null);
    await atualizacao;
    const exclusao = api.delete('contas/10');
    rede.expectOne({ method: 'DELETE', url: '/api/contas/10' }).flush(null);
    await exclusao;
    rede.verify();
  });

  it('mantém o indicador durante concorrência e encerra em erro ou cancelamento', () => {
    vi.useFakeTimers();
    const { http, rede, carregamento } = prepararHttp();
    const primeiro = http.get('/api/um').subscribe();
    const segundo = http.get('/api/dois').subscribe({ error: () => undefined });
    const um = rede.expectOne('/api/um');
    const dois = rede.expectOne('/api/dois');
    expect(carregamento.visivel()).toBe(false);
    vi.advanceTimersByTime(140);
    expect(carregamento.visivel()).toBe(true);
    primeiro.unsubscribe();
    expect(um.cancelled).toBe(true);
    expect(carregamento.visivel()).toBe(true);
    dois.flush(null, { status: 500, statusText: 'Erro' });
    expect(segundo.closed).toBe(true);
    expect(carregamento.visivel()).toBe(false);
  });

  it('ignora assets e outra origem e evita piscar em respostas rápidas', () => {
    vi.useFakeTimers();
    const { http, rede, carregamento } = prepararHttp();
    const urls = ['/imagens/icone.png', 'https://example.net/api/dados'];
    for (const url of urls) http.get(url).subscribe();
    vi.advanceTimersByTime(140);
    expect(carregamento.visivel()).toBe(false);
    for (const url of urls) rede.expectOne(url).flush(null);
    http.get('/api').subscribe();
    rede.expectOne('/api').flush(null);
    vi.advanceTimersByTime(140);
    expect(carregamento.visivel()).toBe(false);
  });

  it('verifica autenticação e preserva falhas diferentes de 401', async () => {
    const { sessao, rede } = prepararHttp();
    const autenticada = sessao.verificar();
    rede.expectOne('/api/sessao').flush({ email: 'conta@example.test' });
    await expect(autenticada).resolves.toBe(true);
    expect(sessao.email()).toBe('conta@example.test');

    const anonima = sessao.verificar();
    rede.expectOne('/api/sessao').flush(null, { status: 401, statusText: 'Sessão expirada' });
    await expect(anonima).resolves.toBe(false);
    expect(sessao.email()).toBeNull();

    const falha = sessao.verificar();
    rede.expectOne('/api/sessao').flush(null, { status: 503, statusText: 'Indisponível' });
    await expect(falha).rejects.toBeInstanceOf(HttpErrorResponse);
  });

  it('envia credenciais apenas para as rotas da sessão', async () => {
    const { sessao, rede } = prepararHttp();
    const dados = { email: 'conta@example.test', senha: 'exemplo' };
    const cadastro = { ...dados, confirmacaoEmail: dados.email, confirmacaoSenha: dados.senha };
    const pedidos = [
      { resposta: sessao.entrar(dados), rota: 'sessao/entrar', corpo: dados },
      { resposta: sessao.registrar(cadastro), rota: 'sessao/registrar', corpo: cadastro },
      { resposta: sessao.gerarCodigo(dados.senha), rota: 'recuperacao-conta/codigo', corpo: { senha: dados.senha } },
      { resposta: sessao.redefinirSenha({ ...dados, codigo: 'ABC', confirmacaoSenha: dados.senha }),
        rota: 'recuperacao-conta/redefinir-senha', corpo: { ...dados, codigo: 'ABC', confirmacaoSenha: dados.senha } }
    ];
    for (const { resposta, rota, corpo } of pedidos) {
      const requisicao = rede.expectOne({ method: 'POST', url: '/api/' + rota });
      expect(requisicao.request.body).toEqual(corpo);
      requisicao.flush(null);
      await resposta;
    }
    const sair = sessao.sair();
    rede.expectOne({ method: 'POST', url: '/api/sessao/sair' }).flush(null);
    await sair;
    const token = sessao.token();
    rede.expectOne({ method: 'GET', url: '/api/sessao/token' }).flush(null);
    await token;
  });
});
