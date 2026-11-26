import { DOCUMENT } from '@angular/common';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { defer, finalize } from 'rxjs';
import { CarregamentoApi } from './carregamento-api.service';

export const carregamentoApiInterceptor: HttpInterceptorFn = (requisicao, proximo) => {
  const documento = inject(DOCUMENT);
  const destino = new URL(requisicao.url, documento.baseURI);
  const mesmaOrigem = destino.origin === documento.location.origin;
  const rotaApi = destino.pathname === '/api' || destino.pathname.startsWith('/api/');

  if (!mesmaOrigem || !rotaApi) return proximo(requisicao);

  const carregamento = inject(CarregamentoApi);
  return defer(() => {
    carregamento.iniciar();
    return proximo(requisicao);
  }).pipe(finalize(() => carregamento.finalizar()));
};
