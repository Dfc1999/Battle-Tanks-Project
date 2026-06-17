import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, RegisterDto, LoginDto } from '../../services/auth.service';

type AuthMode = 'login' | 'register';

@Component({
    selector: 'app-auth',
    standalone: true,
    imports: [FormsModule],
    templateUrl: './auth.component.html',
    styleUrls: ['./auth.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthComponent {
    private router = inject(Router);
    readonly authService = inject(AuthService);

    mode = signal<AuthMode>('login');
    successMessage = signal<string | null>(null);

    loginForm = {
        email: '',
        password: ''
    };

    registerForm = {
        username: '',
        email: '',
        password: '',
        confirmPassword: '',
        firstName: '',
        lastName: ''
    };

    get isLogin(): boolean {
        return this.mode() === 'login';
    }

    get isRegister(): boolean {
        return this.mode() === 'register';
    }

    toggleMode(): void {
        this.mode.update(m => m === 'login' ? 'register' : 'login');
        this.authService.clearError();
        this.successMessage.set(null);
    }

    onLogin(): void {
        if (!this.loginForm.email || !this.loginForm.password) {
            return;
        }

        const dto: LoginDto = {
            email: this.loginForm.email,
            password: this.loginForm.password
        };

        this.authService.login(dto).subscribe({
            next: (response) => {
                if (response.success) {
                    this.successMessage.set('¡Bienvenido!');
                    setTimeout(() => {
                        this.router.navigate(['/menu']);
                    }, 500);
                }
            },
            error: () => {
            }
        });
    }

    onRegister(): void {
        if (!this.validateRegisterForm()) {
            return;
        }

        const dto: RegisterDto = {
            username: this.registerForm.username,
            email: this.registerForm.email,
            password: this.registerForm.password,
            firstName: this.registerForm.firstName || undefined,
            lastName: this.registerForm.lastName || undefined
        };

        this.authService.register(dto).subscribe({
            next: (response) => {
                if (response.success) {
                    this.successMessage.set('Registro exitoso. Inicia sesión.');
                    this.mode.set('login');
                    this.loginForm.email = this.registerForm.email;
                    this.resetRegisterForm();
                }
            },
            error: () => {
            }
        });
    }

    private validateRegisterForm(): boolean {
        if (!this.registerForm.username || !this.registerForm.email || !this.registerForm.password) {
            return false;
        }

        if (this.registerForm.password !== this.registerForm.confirmPassword) {
            this.authService.clearError();
            return false;
        }

        if (this.registerForm.password.length < 6) {
            return false;
        }

        return true;
    }

    private resetRegisterForm(): void {
        this.registerForm = {
            username: '',
            email: '',
            password: '',
            confirmPassword: '',
            firstName: '',
            lastName: ''
        };
    }

    get passwordsMatch(): boolean {
        return this.registerForm.password === this.registerForm.confirmPassword;
    }

    get passwordMinLength(): boolean {
        return this.registerForm.password.length >= 6;
    }
}
