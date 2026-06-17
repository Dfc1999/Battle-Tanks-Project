import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';

export interface ChatMessageDto {
    id: string;
    roomId: string;
    playerId: string;
    playerName: string;
    message: string;
    timestamp: Date;
}

export interface CreateChatMessageDto {
    roomId: string;
    message: string;
}

export interface ChatHistoryResponse {
    roomId: string;
    totalMessages: number;
    messages: ChatMessageDto[];
}

@Injectable({
    providedIn: 'root'
})
export class ChatService {
    private readonly API_URL = 'http://localhost:5013/api/chat';
    private readonly http = inject(HttpClient);

    private messagesSignal = signal<ChatMessageDto[]>([]);
    private isLoadingSignal = signal<boolean>(false);
    private errorSignal = signal<string | null>(null);

    readonly messages = this.messagesSignal.asReadonly();
    readonly isLoading = this.isLoadingSignal.asReadonly();
    readonly error = this.errorSignal.asReadonly();

    getChatHistory(roomId: string, limit: number = 50): Observable<ChatHistoryResponse> {
        this.isLoadingSignal.set(true);
        this.errorSignal.set(null);

        return this.http.get<ChatHistoryResponse>(`${this.API_URL}/${roomId}?limit=${limit}`).pipe(
            tap(response => {
                this.isLoadingSignal.set(false);
                this.messagesSignal.set(response.messages);
                console.log(`💬 ${response.totalMessages} mensajes cargados`);
            }),
            catchError(error => {
                this.isLoadingSignal.set(false);
                const message = error.error?.message || 'Error al cargar historial de chat';
                this.errorSignal.set(message);
                console.error('❌ Error cargando chat:', message);
                return throwError(() => new Error(message));
            })
        );
    }

    sendMessage(dto: CreateChatMessageDto): Observable<ChatMessageDto> {
        this.errorSignal.set(null);

        return this.http.post<ChatMessageDto>(this.API_URL, dto).pipe(
            tap(message => {
                this.messagesSignal.update(msgs => [...msgs, message]);
                console.log(`✅ Mensaje persistido: ${message.message.substring(0, 20)}...`);
            }),
            catchError(error => {
                const message = error.error?.message || 'Error al enviar mensaje';
                this.errorSignal.set(message);
                console.error('❌ Error enviando mensaje:', message);
                return throwError(() => new Error(message));
            })
        );
    }

    addLocalMessage(message: ChatMessageDto): void {
        this.messagesSignal.update(msgs => [...msgs, message]);
    }

    clearMessages(): void {
        this.messagesSignal.set([]);
    }

    clearError(): void {
        this.errorSignal.set(null);
    }
}

