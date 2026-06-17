import { Component, OnInit, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

interface LeaderboardEntry {
    rank: number;
    playerName: string;
    points: number;
}

@Component({
    selector: 'app-main-menu',
    standalone: true,
    imports: [],
    templateUrl: './main-menu.component.html',
    styleUrls: ['./main-menu.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainMenuComponent implements OnInit {
    private readonly router = inject(Router);
    private readonly http = inject(HttpClient);
    readonly authService = inject(AuthService);

    leaderboard = signal<LeaderboardEntry[]>([]);
    isLoadingLeaderboard = signal(false);

    get username(): string {
        return this.authService.currentUser()?.username || 'Soldado';
    }

    get totalScore(): number {
        return this.authService.currentUser()?.totalScore || 0;
    }

    get gamesPlayed(): number {
        return this.authService.currentUser()?.gamesPlayed || 0;
    }

    get wins(): number {
        return this.authService.currentUser()?.wins || 0;
    }

    ngOnInit(): void {
        this.loadCurrentUser();
        this.loadLeaderboard();
    }

    private loadCurrentUser(): void {
        this.authService.getCurrentUser().subscribe({
            next: () => console.log('Datos del usuario cargados'),
            error: (err) => console.error('Error cargando usuario:', err)
        });
    }

    private loadLeaderboard(): void {
        this.isLoadingLeaderboard.set(true);
        this.http.get<LeaderboardEntry[]>('http://localhost:5013/api/scores/leaderboard?top=10').subscribe({
            next: (data) => {
                this.leaderboard.set(data);
                this.isLoadingLeaderboard.set(false);
            },
            error: (err) => {
                console.error('Error cargando leaderboard:', err);
                this.isLoadingLeaderboard.set(false);
            }
        });
    }

    goToLobby(): void {
        this.router.navigate(['/lobby']);
    }

    logout(): void {
        this.authService.logout();
        this.router.navigate(['/auth']);
    }
}
