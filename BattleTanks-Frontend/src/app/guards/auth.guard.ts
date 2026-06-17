import { inject }          from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService }     from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, _state) => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (authService.isTokenValid()) {
    return true;
  }

  router.navigate(['/auth']);
  return false;
};