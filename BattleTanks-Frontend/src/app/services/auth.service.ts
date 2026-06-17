import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';

export interface RegisterDto {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface PlayerAuthDto {
  id: string;
  firebaseUid: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  totalScore: number;
  gamesPlayed: number;
  wins: number;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  player?: PlayerAuthDto;
}

interface FirebaseJwtPayload {
  exp: number;       
  iat: number;      
  sub: string;     
  email?: string;
  [key: string]: unknown;
}

const AUTH_TOKEN_KEY = 'battletanks_token';
const AUTH_USER_KEY  = 'battletanks_user';

const EXPIRY_BUFFER_SECONDS = 60;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API_URL = 'http://localhost:5013/api/auth';
  private readonly http    = inject(HttpClient);

  private readonly currentUserSignal  = signal<PlayerAuthDto | null>(null);
  private readonly isLoadingSignal    = signal<boolean>(false);
  private readonly errorSignal        = signal<string | null>(null);

  readonly currentUser      = this.currentUserSignal.asReadonly();
  readonly isLoading        = this.isLoadingSignal.asReadonly();
  readonly error            = this.errorSignal.asReadonly();
  readonly isAuthenticated  = computed(() => !!this.currentUserSignal() && this.isTokenValid());

  constructor() {
    this.loadUserFromStorage();
  }

  isTokenValid(): boolean {
    const token = this.getToken();
    if (!token) return false;

    const payload = this.decodeToken(token);
    if (!payload) return false;

    const nowInSeconds   = Math.floor(Date.now() / 1000);
    const isExpired      = payload.exp <= nowInSeconds + EXPIRY_BUFFER_SECONDS;

    if (isExpired) {
      this.clearStorage();
    }

    return !isExpired;
  }

  getTokenExpirationDate(): Date | null {
    const token = this.getToken();
    if (!token) return null;

    const payload = this.decodeToken(token);
    if (!payload) return null;

    return new Date(payload.exp * 1000);
  }

  private decodeToken(token: string): FirebaseJwtPayload | null {
    try {
      const parts = token.split('.');

      if (parts.length !== 3) return null;

      const base64Url = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const jsonStr   = atob(base64Url);

      return JSON.parse(jsonStr) as FirebaseJwtPayload;
    } catch {
      return null;
    }
  }

  getToken(): string | null {
    return localStorage.getItem(AUTH_TOKEN_KEY);
  }

  private loadUserFromStorage(): void {
    const userJson = localStorage.getItem(AUTH_USER_KEY);
    if (!userJson) return;

    try {
      const user = JSON.parse(userJson) as PlayerAuthDto;
      this.currentUserSignal.set(user);
    } catch {
      this.clearStorage();
    }
  }

  private saveToStorage(token: string, user: PlayerAuthDto): void {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(user));
    this.currentUserSignal.set(user);
  }

  clearStorage(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_USER_KEY);
    this.currentUserSignal.set(null);
  }

  register(dto: RegisterDto): Observable<AuthResponse> {
    this.isLoadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.post<AuthResponse>(`${this.API_URL}/register`, dto).pipe(
      tap(response => {
        this.isLoadingSignal.set(false);
        if (!response.success) {
          this.errorSignal.set(response.message);
        }
      }),
      catchError(error => {
        this.isLoadingSignal.set(false);
        const message = error.error?.message ?? 'Error en el registro';
        this.errorSignal.set(message);
        return throwError(() => new Error(message));
      })
    );
  }

  login(dto: LoginDto): Observable<AuthResponse> {
    this.isLoadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.post<AuthResponse>(`${this.API_URL}/login`, dto).pipe(
      tap(response => {
        this.isLoadingSignal.set(false);
        if (response.success && response.token && response.player) {
          this.saveToStorage(response.token, response.player);
        } else {
          this.errorSignal.set(response.message);
        }
      }),
      catchError(error => {
        this.isLoadingSignal.set(false);
        const message = error.error?.message ?? 'Credenciales inválidas';
        this.errorSignal.set(message);
        return throwError(() => new Error(message));
      })
    );
  }

  getCurrentUser(): Observable<PlayerAuthDto> {
    this.isLoadingSignal.set(true);

    return this.http.get<PlayerAuthDto>(`${this.API_URL}/me`).pipe(
      tap(user => {
        this.isLoadingSignal.set(false);
        this.currentUserSignal.set(user);
      }),
      catchError(error => {
        this.isLoadingSignal.set(false);
        this.clearStorage();
        return throwError(() => error);
      })
    );
  }

  logout(): void {
    this.clearStorage();
    this.errorSignal.set(null);
  }

  clearError(): void {
    this.errorSignal.set(null);
  }
}