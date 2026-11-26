import { describe, expect, it } from 'vitest';
import { criarTela, estabilizar } from '../../test/fixtures';
import { appConfig } from './app.config';
import { AppComponent } from './app.component';
import { routes } from './app.routes';

describe('shell e rotas', () => {
  it('carrega separadamente cada tela registrada', async () => {
    const paginas = routes.flatMap(rota => rota.children ?? [rota]).filter(rota => rota.loadComponent);
    const componentes = await Promise.all(paginas.map(rota => rota.loadComponent?.()));
    expect(componentes.map(componente => (componente as { name: string }).name)).toEqual([
      'SessaoComponent', 'CadastroUsuarioComponent', 'RedefinirSenhaComponent', 'SegurancaComponent',
      'DashboardComponent', 'CadastrosComponent', 'LancamentosComponent', 'RecorrenciasComponent', 'CartoesComponent'
    ]);
    expect(appConfig.providers).toHaveLength(3);
  });

  it('apresenta a navegação para a sessão ativa e limpa o estado ao sair', async () => {
    const { tela, fixture, sessao, navegar } = await criarTela(AppComponent);
    expect(fixture.nativeElement.querySelector('nav')).toBeNull();
    sessao.email.set('conta@example.test');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('nav').textContent).toContain('Cartões e parcelas');
    tela.sair();
    await estabilizar(fixture);
    expect(sessao.sair).toHaveBeenCalledOnce();
    expect(sessao.email()).toBeNull();
    expect(navegar).toHaveBeenCalledWith('/entrar');
  });
});
