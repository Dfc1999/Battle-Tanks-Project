import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';

export interface RoomPlayerDto {
    playerId: string;
    username: string;
    joinedAt: Date;
}

export interface RoomDto {
    id: string;
    name: string;
    selectedMap: string;
    status: string;
    maxPlayers: number;
    currentPlayers: number;
    createdAt: Date;
    players: RoomPlayerDto[];
}

export interface CreateRoomDto {
    name: string;
    selectedMap?: string;
    maxPlayers?: number;
}

export interface JoinRoomResponse {
    success: boolean;
    message: string;
    room?: RoomDto;
}

@Injectable({
    providedIn: 'root'
})
export class RoomService {
    private readonly API_URL = 'http://localhost:5013/api/room';
    private readonly http = inject(HttpClient);

    private roomsSignal = signal<RoomDto[]>([]);
    private currentRoomSignal = signal<RoomDto | null>(null);
    private isLoadingSignal = signal<boolean>(false);
    private errorSignal = signal<string | null>(null);

    readonly rooms = this.roomsSignal.asReadonly();
    readonly currentRoom = this.currentRoomSignal.asReadonly();
    readonly isLoading = this.isLoadingSignal.asReadonly();
    readonly error = this.errorSignal.asReadonly();

    getAvailableRooms(): Observable<RoomDto[]> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set(null);

        return this.http.get<RoomDto[]>(this.API_URL).pipe(
            tap(rooms => {
                this.isLoadingSignal.set(false);
                this.roomsSignal.set(rooms);
                console.log(`📋 ${rooms.length} salas disponibles`);
            }),
            catchError(error => {
                this.isLoadingSignal.set(false);
                const message = error.error?.message || 'Error al obtener salas';
                this.errorSignal.set(message);
                console.error('❌ Error obteniendo salas:', message);
                return throwError(() => new Error(message));
            })
        );
    }

    getRoomById(id: string): Observable<RoomDto> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set(null);

        return this.http.get<RoomDto>(`${this.API_URL}/${id}`).pipe(
            tap(room => {
                this.isLoadingSignal.set(false);
                this.currentRoomSignal.set(room);
                console.log(`🏠 Sala obtenida: ${room.name}`);
            }),
            catchError(error => {
                this.isLoadingSignal.set(false);
                const message = error.error?.message || 'Sala no encontrada';
                this.errorSignal.set(message);
                console.error('❌ Error obteniendo sala:', message);
                return throwError(() => new Error(message));
            })
        );
    }

    createRoom(dto: CreateRoomDto): Observable<RoomDto> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set(null);

        return this.http.post<RoomDto>(this.API_URL, dto).pipe(
            tap(room => {
                this.isLoadingSignal.set(false);
                this.currentRoomSignal.set(room);
                this.roomsSignal.update(rooms => [...rooms, room]);
                console.log(`✅ Sala creada: ${room.name}`);
            }),
            catchError(error => {
                this.isLoadingSignal.set(false);
                const message = error.error?.message || 'Error al crear sala';
                this.errorSignal.set(message);
                console.error('❌ Error creando sala:', message);
                return throwError(() => new Error(message));
            })
        );
    }

    joinRoom(id: string): Observable<JoinRoomResponse> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set(null);

        return this.http.put<JoinRoomResponse>(`${this.API_URL}/${id}/join`, {}).pipe(
            tap(response => {
                this.isLoadingSignal.set(false);
                if (response.success && response.room) {
                    this.currentRoomSignal.set(response.room);
                    console.log(`✅ Unido a sala: ${response.room.name}`);
                } else {
                    this.errorSignal.set(response.message);
                }
            }),
            catchError(error => {
                this.isLoadingSignal.set(false);
                const message = error.error?.message || 'Error al unirse a la sala';
                this.errorSignal.set(message);
                console.error('❌ Error uniéndose a sala:', message);
                return throwError(() => new Error(message));
            })
        );
    }

    leaveRoom(id: string): Observable<JoinRoomResponse> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set(null);

        return this.http.put<JoinRoomResponse>(`${this.API_URL}/${id}/leave`, {}).pipe(
            tap(response => {
                this.isLoadingSignal.set(false);
                if (response.success) {
                    this.currentRoomSignal.set(null);
                    console.log('👋 Sala abandonada');
                } else {
                    this.errorSignal.set(response.message);
                }
            }),
            catchError(error => {
                this.isLoadingSignal.set(false);
                const message = error.error?.message || 'Error al abandonar sala';
                this.errorSignal.set(message);
                console.error('❌ Error abandonando sala:', message);
                return throwError(() => new Error(message));
            })
        );
    }

    clearCurrentRoom(): void {
        this.currentRoomSignal.set(null);
    }

    clearError(): void {
        this.errorSignal.set(null);
    }

    deleteRoom(id: string): Observable<any> {
        return this.http.delete(`${this.API_URL}/${id}`).pipe(
            tap(() => {
                this.currentRoomSignal.set(null);
                this.roomsSignal.update(rooms => rooms.filter(r => r.id !== id));
                console.log(`🗑️ Sala eliminada: ${id}`);
            }),
            catchError(error => {
                console.error('❌ Error eliminando sala:', error);
                // Non-blocking: even if delete fails, continue navigation
                this.currentRoomSignal.set(null);
                return throwError(() => new Error('Error al eliminar sala'));
            })
        );
    }
}
