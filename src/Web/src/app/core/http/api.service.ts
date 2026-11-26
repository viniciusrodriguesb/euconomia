import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  get<T>(rota: string, params: Record<string, string | number> = {}) {
    return firstValueFrom(this.http.get<T>(`/api/${rota}`, { params: new HttpParams({ fromObject: params }) }));
  }
  post<T = void>(rota: string, body: unknown = {}) {
    return firstValueFrom(this.http.post<T>(`/api/${rota}`, body));
  }
  put<T = void>(rota: string, body: unknown = {}) {
    return firstValueFrom(this.http.put<T>(`/api/${rota}`, body));
  }
  delete(rota: string) {
    return firstValueFrom(this.http.delete<void>(`/api/${rota}`));
  }
}
