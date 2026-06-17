import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, inject, effect, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { GameService, Player, ChatMessage } from '../../services/game.service';
import { AuthService } from '../../services/auth.service';
import { RoomService, RoomDto } from '../../services/room.service';
import { ChatService, CreateChatMessageDto } from '../../services/chat.service';
import { Subscription } from 'rxjs';
import { PlayerStore, PlayerState } from '../../store/player.store';
import { ChatStore, ChatMessageState } from '../../store/chat.store';
import { TANK_TYPES, TankType } from '../../models/game.models';
import { PlayerIdentityService } from '../../services/player-identity.service';

@Component({
  selector: 'app-waiting-room',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './waiting-room.component.html',
  styleUrls: ['./waiting-room.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WaitingRoomComponent implements OnInit, OnDestroy {
  readonly store = inject(PlayerStore);
  readonly chatStore = inject(ChatStore);
  readonly authService = inject(AuthService);
  readonly roomService = inject(RoomService);
  readonly chatService = inject(ChatService);
  private readonly playerIdentity = inject(PlayerIdentityService);

  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private ngZone = inject(NgZone);

  currentPlayer: Player = {
    id: '',
    name: '',
    isReady: false
  };

  currentRoom: RoomDto | null = null;
  newMessage: string = '';
  roomId: string = '';
  errorMessage: string | null = null;
  countdown: number | null = null;
  isStarting: boolean = false;
  private subscriptions: Subscription[] = [];
  private countdownInterval: any = null;

  tankTypes = TANK_TYPES;
  selectedTankId: string = 'e100';

  get emptySlots(): number[] {
    const emptyCount = Math.max(0, (this.currentRoom?.maxPlayers || 4) - this.store.players().length);
    return Array(emptyCount).fill(0).map((_, i) => i);
  }

  get allPlayersReady(): boolean {
    const players = this.store.players();
    return players.length >= 2 && players.every(p => p.isReady);
  }

  constructor(private gameService: GameService) {
    effect(() => {
      console.log('Estado del Store actualizado:', this.store.players());
    });
  }

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/auth']);
      return;
    }

    this.route.queryParams.subscribe(params => {
      this.roomId = params['roomId'] || '';

      if (this.roomId) {
        this.loadRoomDetails();
      } else {
        this.errorMessage = 'No se especificó una sala';
      }
    });

    this.initializePlayer();
    this.setupWebSocketListeners();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.chatStore.clearMessages();
    this.clearCountdown();
    this.gameService.relinquish();       
  }

  private initializePlayer(): void {
    const identity = this.playerIdentity.getIdentityOrThrow();

    this.currentPlayer.id   = identity.dbId;
    this.currentPlayer.name = identity.username;

    this.addToStore(this.currentPlayer);
  }

  private loadRoomDetails(): void {
    this.roomService.getRoomById(this.roomId).subscribe({
      next: (room) => {
        this.currentRoom = room;
        console.log('Sala cargada:', room.name);
        this.loadChatHistory();
        this.connectToRoom();
      },
      error: (err) => {
        this.errorMessage = 'No se pudo cargar la sala';
        console.error('Error cargando sala:', err);
      }
    });
  }

  private loadChatHistory(): void {
    this.chatService.getChatHistory(this.roomId).subscribe({
      next: (response) => {
        const messages = response.messages.map(m => ({
          playerId: m.playerId,
          playerName: m.playerName,
          message: m.message,
          timestamp: new Date(m.timestamp)
        }));
        this.chatStore.setMessages(messages);
        console.log(`Historial cargado: ${response.totalMessages} mensajes`);
        this.scrollChatToBottom();
      },
      error: (err) => {
        console.warn('No se pudo cargar historial de chat:', err);
      }
    });
  }

  private connectToRoom(): void {
    this.gameService.acquire();          
    this.gameService.connect(
      this.roomId,
      this.currentPlayer.id,
      this.currentPlayer.name
    ).then(() => {
      console.log('Conectado a la sala:', this.roomId);
      this.errorMessage = null;
    }).catch(err => {
      console.error('Error al conectar:', err);
      this.errorMessage = 'No se pudo conectar al servidor de juego';
      this.gameService.relinquish();    
    });
  }

  private setupWebSocketListeners(): void {
    const currentPlayersSub = this.gameService.onCurrentPlayers().subscribe({
      next: (players) => {
        this.ngZone.run(() => {
          const storePlayers: PlayerState[] = players.map(p => ({
            id: p.id,
            name: p.name,
            isReady: p.isReady,
            x: 0,
            y: 0,
            rotation: 0,
            health: 3,
            maxHealth: 3,
            color: '#2196F3',
            score: 0,
            ammo: 10,
            maxAmmo: 10,
            status: 'alive' as const,
            tankType: 'e100'
          }));
          this.store.setPlayers(storePlayers);
        });
      }
    });

    const playerJoinedSub = this.gameService.onPlayerJoined().subscribe({
      next: (player) => {
        this.ngZone.run(() => {
          if (player.id !== this.currentPlayer.id) {
            this.addToStore(player);
            this.addSystemMessage(`${player.name} se ha unido a la sala`);
          }
        });
      }
    });

    const playerLeftSub = this.gameService.onPlayerLeft().subscribe({
      next: (playerId) => {
        this.ngZone.run(() => {
          const player = this.store.players().find(p => p.id === playerId);
          this.store.removePlayer(playerId);

          if (player) {
            this.addSystemMessage(`${player.name} ha abandonado la sala`);
          }

          this.handlePlayerDisconnection();
        });
      }
    }); 

    const chatSub = this.gameService.onChatMessage().subscribe({
      next: (message) => {
        this.chatStore.addMessage({
          playerId: message.playerId,
          playerName: message.playerName,
          message: message.message,
          timestamp: message.timestamp
        });
        this.scrollChatToBottom();
      }
    });

    // Escuchar cambios de estado Listo de otros jugadores
    const playerReadySub = this.gameService.onPlayerReady().subscribe({
      next: (data) => {
        this.ngZone.run(() => {
          console.log(`PLAYER_READY recibido en waiting-room:`, data);
          console.log(`Jugadores actuales en store:`, this.store.players().map(p => ({ id: p.id, name: p.name, isReady: p.isReady })));
          console.log(`Buscando jugador con ID: ${data.playerId}`);

          const existingPlayer = this.store.players().find(p => p.id === data.playerId);
          console.log(`Jugador encontrado:`, existingPlayer ? `${existingPlayer.name}` : 'NO ENCONTRADO');

          this.store.updatePlayerReady(data.playerId, data.isReady);

          console.log(`Estado después de actualizar:`, this.store.players().map(p => ({ id: p.id, name: p.name, isReady: p.isReady })));

          this.checkAllPlayersReady();
        });
      }
    });

    const gameStartedSub = this.gameService.onGameStarted().subscribe({
      next: (data) => {
        this.ngZone.run(() => {
          console.log('¡El juego ha iniciado!');
          this.router.navigate(['/game'], {
            queryParams: { roomId: this.roomId }
          });
        });
      }
    });

    const tankSelectedSub = this.gameService.onTankSelected().subscribe({
      next: (data) => {
        this.ngZone.run(() => {
          console.log(`Tanque seleccionado por ${data.playerId}: ${data.tankType}`);
          this.store.updateTankType(data.playerId, data.tankType);
        });
      }
    });

    this.subscriptions.push(currentPlayersSub, playerJoinedSub, playerLeftSub, chatSub, playerReadySub, gameStartedSub, tankSelectedSub);
  }

  private addToStore(player: Player): void {
    this.store.addPlayer({
      id: player.id,
      name: player.name,
      isReady: player.isReady,
      color: player.id === this.currentPlayer.id ? '#4CAF50' : '#F44336'
    });
  }

  toggleReady(): void {
    this.currentPlayer.isReady = !this.currentPlayer.isReady;
    this.store.updatePlayerReady(this.currentPlayer.id, this.currentPlayer.isReady);

    this.gameService.sendPlayerReady(this.currentPlayer.id, this.currentPlayer.isReady);
    console.log(`Enviando estado listo: ${this.currentPlayer.isReady}`);

    this.checkAllPlayersReady();
  }

  private checkAllPlayersReady(): void {
    if (this.allPlayersReady && !this.isStarting) {
      console.log('¡Todos los jugadores están listos!');
      this.startCountdown();
    }
  }

  private startCountdown(): void {
    if (this.isStarting) return;

    this.isStarting = true;
    this.countdown = 3;
    this.addSystemMessage('¡Todos listos! El juego comenzará en 3...');

    this.countdownInterval = setInterval(() => {
      this.ngZone.run(() => {
        if (this.countdown !== null && this.countdown > 1) {
          this.countdown--;
          this.addSystemMessage(`${this.countdown}...`);
        } else {
          this.clearCountdown();
          this.addSystemMessage('¡GO!');

          this.gameService.sendGameStart(this.roomId);
          this.router.navigate(['/game'], {
            queryParams: { roomId: this.roomId }
          });
        }
      });
    }, 1000);
  }

  private clearCountdown(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
    this.countdown = null;
  }

  private handlePlayerDisconnection(): void {
    const wasCountingDown = this.isStarting;

    this.clearCountdown();       
    this.isStarting = false;     

    if (wasCountingDown) {
      this.addSystemMessage('⚠️ Inicio cancelado: un jugador se desconectó');
    }

    this.checkAllPlayersReady();
  }

  sendMessage(): void {
    if (this.newMessage.trim()) {
      const messageText = this.newMessage;
      this.newMessage = '';

      this.gameService.sendChatMessage(
        messageText,
        this.currentPlayer.id,
        this.currentPlayer.name
      );

      if (this.authService.isAuthenticated() && this.roomId) {
        const dto: CreateChatMessageDto = {
          roomId: this.roomId,
          message: messageText
        };
        this.chatService.sendMessage(dto).subscribe({
          next: () => console.log('Mensaje persistido en API'),
          error: (err) => console.warn('No se pudo persistir mensaje:', err)
        });
      }
    }
  }

  private addSystemMessage(text: string): void {
    this.chatStore.addSystemMessage(text);
    this.scrollChatToBottom();
  }

  private scrollChatToBottom(): void {
    setTimeout(() => {
      const chatContainer = document.querySelector('.chat-messages');
      if (chatContainer) {
        chatContainer.scrollTop = chatContainer.scrollHeight;
      }
    }, 100);
  }

  startGame(): void {
    if (this.allPlayersReady) {
      console.log('¡Iniciando juego con:', this.store.players());
      this.router.navigate(['/game'], {
        queryParams: { roomId: this.roomId }
      });
    }
  }

  leaveRoom(): void {
    if (this.roomId) {
      this.roomService.leaveRoom(this.roomId).subscribe({
        next: () => {
          this.router.navigate(['/lobby']);
        },
        error: () => {
          this.router.navigate(['/lobby']);
        }
      });
    } else {
      this.router.navigate(['/lobby']);
    }
  }

  selectTank(tankId: string): void {
    this.selectedTankId = tankId;
    this.store.updateTankType(this.currentPlayer.id, tankId);
    this.gameService.sendTankSelection(this.currentPlayer.id, tankId);
    console.log(`Tanque seleccionado: ${tankId}`);
  }

  getTankType(playerId: string): TankType | undefined {
    const player = this.store.players().find(p => p.id === playerId);
    const tankId = player?.tankType || 'e100';
    return TANK_TYPES.find(t => t.id === tankId);
  }

}
