import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Tela } from './core/ui/tela';
import { MENU_PRINCIPAL } from './menu-principal';
import { CarregamentoApiComponent } from './shared/carregamento/carregamento-api.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CarregamentoApiComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.component.html'
})
export class AppComponent extends Tela {
  readonly sessao = this.sessaoService;
  private readonly router = inject(Router);
  readonly menu = MENU_PRINCIPAL;

  sair() {
    void this.executar(async () => {
      await this.sessao.sair();
      await this.router.navigateByUrl('/entrar');
    });
  }
}
