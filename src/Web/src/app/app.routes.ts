import { Routes } from '@angular/router';
import { exigirSessao } from './core/auth/sessao.guard';

export const routes: Routes = [
  { path: 'entrar', title: 'Entrar · Euconomia', loadComponent: () => import('./features/sessao/sessao.component').then(m => m.SessaoComponent) },
  { path: 'criar-conta', title: 'Criar conta · Euconomia', loadComponent: () => import('./features/cadastro-usuario/cadastro-usuario.component').then(m => m.CadastroUsuarioComponent) },
  { path: 'redefinir-senha', title: 'Redefinir senha · Euconomia', loadComponent: () => import('./features/redefinir-senha/redefinir-senha.component').then(m => m.RedefinirSenhaComponent) },
  {
    path: '', canActivateChild: [exigirSessao], children: [
      { path: 'seguranca', title: 'Segurança · Euconomia', loadComponent: () => import('./features/seguranca/seguranca.component').then(m => m.SegurancaComponent) },
      { path: '', title: 'Visão geral · Euconomia', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'cadastros', title: 'Cadastros · Euconomia', loadComponent: () => import('./features/cadastros/cadastros.component').then(m => m.CadastrosComponent) },
      { path: 'lancamentos', title: 'Lançamentos · Euconomia', loadComponent: () => import('./features/lancamentos/lancamentos.component').then(m => m.LancamentosComponent) },
      { path: 'recorrencias', title: 'Recorrências · Euconomia', loadComponent: () => import('./features/recorrencias/recorrencias.component').then(m => m.RecorrenciasComponent) },
      { path: 'cartoes', title: 'Cartões e parcelas · Euconomia', loadComponent: () => import('./features/cartoes/cartoes.component').then(m => m.CartoesComponent) },
    ],
  },
  { path: '**', redirectTo: '' }
];
