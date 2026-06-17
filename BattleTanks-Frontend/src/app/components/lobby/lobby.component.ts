import { Component, OnInit, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { RoomService, RoomDto, CreateRoomDto } from '../../services/room.service';

@Component({
    selector: 'app-lobby',
    standalone: true,
    imports: [FormsModule, DatePipe],
    templateUrl: './lobby.component.html',
    styleUrls: ['./lobby.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LobbyComponent implements OnInit {
    private router = inject(Router);
    readonly authService = inject(AuthService);
    readonly roomService = inject(RoomService);

    showCreateForm = signal(false);
    selectedRoom = signal<RoomDto | null>(null);

    // Only show rooms that are still waiting for players
    activeRooms = computed(() =>
        this.roomService.rooms().filter(r => r.status.toLowerCase() === 'waiting')
    );

    createRoomForm: CreateRoomDto = {
        name: '',
        selectedMap: 'default',
        maxPlayers: 4
    };

    availableMaps = [
        { id: 'default', name: 'Mapa Clásico' },
        { id: 'desert', name: 'Desierto' },
        { id: 'forest', name: 'Bosque' }
    ];

    ngOnInit(): void {
        if (!this.authService.isAuthenticated()) {
            this.router.navigate(['/auth']);
            return;
        }

        this.loadRooms();
    }

    loadRooms(): void {
        this.roomService.getAvailableRooms().subscribe({
            next: () => {
                console.log('Salas cargadas');
            },
            error: (err) => {
                console.error('Error cargando salas:', err);
            }
        });
    }

    toggleCreateForm(): void {
        this.showCreateForm.update(v => !v);
        if (!this.showCreateForm()) {
            this.resetCreateForm();
        }
    }

    createRoom(): void {
        if (!this.createRoomForm.name.trim()) {
            return;
        }

        this.roomService.createRoom(this.createRoomForm).subscribe({
            next: (room) => {
                this.showCreateForm.set(false);
                this.resetCreateForm();
                this.router.navigate(['/waiting-room'], {
                    queryParams: { roomId: room.id }
                });
            },
            error: (err) => {
                console.error('Error creando sala:', err);
            }
        });
    }

    joinRoom(room: RoomDto): void {
        this.roomService.joinRoom(room.id).subscribe({
            next: (response) => {
                if (response.success) {
                    this.router.navigate(['/waiting-room'], {
                        queryParams: { roomId: room.id }
                    });
                }
            },
            error: (err) => {
                console.error('Error uniéndose a sala:', err);
            }
        });
    }

    selectRoom(room: RoomDto): void {
        this.selectedRoom.set(room);
    }

    logout(): void {
        this.authService.logout();
        this.router.navigate(['/auth']);
    }

    refreshRooms(): void {
        this.loadRooms();
    }

    private resetCreateForm(): void {
        this.createRoomForm = {
            name: '',
            selectedMap: 'default',
            maxPlayers: 4
        };
    }

    getStatusClass(status: string): string {
        switch (status.toLowerCase()) {
            case 'waiting': return 'status-waiting';
            case 'playing': return 'status-playing';
            case 'finished': return 'status-finished';
            default: return '';
        }
    }

    getStatusLabel(status: string): string {
        switch (status.toLowerCase()) {
            case 'waiting': return '⏳ Esperando';
            case 'playing': return '🎮 En juego';
            case 'finished': return '🏁 Terminada';
            default: return status;
        }
    }

    getStatusBadgeClass(status: string): string {
        switch (status.toLowerCase()) {
            case 'waiting': return 'warning';
            case 'playing': return 'success';
            case 'finished': return 'error';
            default: return 'dark';
        }
    }
}
