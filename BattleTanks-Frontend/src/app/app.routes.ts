import { Routes } from '@angular/router';
import { AuthComponent } from './components/auth/auth.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/auth',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    component: AuthComponent,
    title: 'Login - Battle Tanks'
  },
  {
    path: 'menu',
    loadComponent: () => import('./components/main-menu/main-menu.component').then(m => m.MainMenuComponent),
    canActivate: [authGuard],
    title: 'Menú Principal - Battle Tanks'
  },
  {
    path: 'lobby',
    loadComponent: () => import('./components/lobby/lobby.component').then(m => m.LobbyComponent),
    canActivate: [authGuard],
    title: 'Lobby - Battle Tanks'
  },
  {
    path: 'waiting-room',
    loadComponent: () => import('./components/waiting-room/waiting-room.component').then(m => m.WaitingRoomComponent),
    canActivate: [authGuard],
    title: 'Sala de Espera - Battle Tanks'
  },
  {
    path: 'game',
    loadComponent: () => import('./components/game-canvas/game-canvas.component').then(m => m.GameCanvasComponent),
    canActivate: [authGuard],
    title: 'Juego - Battle Tanks'
  },
  {
    path: '**',
    redirectTo: '/auth'
  }
];
