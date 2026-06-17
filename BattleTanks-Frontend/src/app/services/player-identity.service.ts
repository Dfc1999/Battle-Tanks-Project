import { Injectable, computed, inject } from '@angular/core';
import { AuthService } from './auth.service';

export interface PlayerIdentity {
  readonly dbId: string;
  readonly username: string;
}

@Injectable({ providedIn: 'root' })
export class PlayerIdentityService {
  private readonly authService = inject(AuthService);

  readonly identity = computed<PlayerIdentity | null>(() => {
    const user = this.authService.currentUser();
    if (!user || !this.authService.isTokenValid()) return null;

    return {
      dbId:     user.id,
      username: user.username,
    };
  });

  readonly hasValidIdentity = computed(() => this.identity() !== null);

  getIdentityOrThrow(): PlayerIdentity {
    const identity = this.identity();

    if (!identity) {
      throw new Error(
        '[PlayerIdentityService] Se intentó acceder a la identidad sin sesión activa. ' +
        'Asegúrate de que la ruta está protegida por authGuard.'
      );
    }

    return identity;
  }
}