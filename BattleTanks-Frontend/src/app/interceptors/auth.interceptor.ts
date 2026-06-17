import { inject }              from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { Router }              from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService }         from '../services/auth.service';

const API_BASE = 'http://localhost:5013';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(API_BASE)) {
    return next(req);
  }

  const authService = inject(AuthService);
  const router      = inject(Router);
  const token       = authService.getToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        authService.clearStorage();
        router.navigate(['/auth']);
      }
      return throwError(() => error);
    })
  );
};