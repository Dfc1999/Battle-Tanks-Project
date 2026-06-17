import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { BenchmarkService } from './benchmark.service';
import { PowerUp } from '../models/game.models';

export interface PlayerPosition {
  playerId: string;
  x: number;
  y: number;
  rotation: number;
}

export interface ChatMessage {
  playerId: string;
  playerName: string;
  message: string;
  timestamp: Date;
}

export interface Player {
  id: string;
  name: string;
  isReady: boolean;
}

export interface GameMessage {
  type: string;
  data: any;
}

@Injectable({
  providedIn: 'root'
})
export class GameService {
  private hubConnection: signalR.HubConnection | null = null;
  private consumerCount = 0;
  private readonly HUB_URL = 'http://localhost:5013/game';
  private readonly benchmarkService = inject(BenchmarkService);

  private playerMoveSubject = new Subject<PlayerPosition>();
  private chatMessageSubject = new Subject<ChatMessage>();
  private playerJoinedSubject = new Subject<Player>();
  private playerLeftSubject = new Subject<string>();
  private currentPlayersSubject = new Subject<Player[]>();
  private obstacleDestroyedSubject = new Subject<string>();
  private playerReadySubject = new Subject<{ playerId: string, isReady: boolean }>();
  private gameStartedSubject = new Subject<{ roomId: string, players: Player[] }>();
  private playerHitSubject = new Subject<{ targetPlayerId: string, shooterId: string, damage: number }>();
  private tankSelectedSubject = new Subject<{ playerId: string, tankType: string }>();
  private bulletFiredSubject = new Subject<{ ownerId: string, x: number, y: number, rotation: number }>();

  // Subjects para power-ups
  private powerUpSpawnedSubject = new Subject<PowerUp>();
  private collectPowerUpSubject = new Subject<{ powerUpId: string, playerId: string, type: string }>();
  private gameOverSubject = new Subject<{ eliminatedPlayerId: string, eliminatedBy: string }>();
  private matchEndedSubject = new Subject<{ roomId: string, winnerId: string, winnerName: string, players: { playerId: string, playerName: string, isWinner: boolean }[] }>();
  private eventHistorySubject = new Subject<any[]>();

  constructor() { }

  connect(roomId: string, playerId: string, playerName: string): Promise<void> {
    if (this.hubConnection) {
      console.log('Ya existe una conexion activa');
      return Promise.resolve();
    }

    const url = `${this.HUB_URL}?roomId=${roomId}&playerId=${playerId}&playerName=${playerName}`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupListeners();

    return this.hubConnection
      .start()
      .then(() => {
        console.log('Conectado al Hub de SignalR');
      })
      .catch(err => {
        console.error('Error conectando al Hub:', err);
        throw err;
      });
  }

  private setupListeners(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveMessage', (message: GameMessage) => {
      console.log('Mensaje recibido:', message);
      this.handleMessage(message);
    });

    // Recibir historial de eventos de Redis
    this.hubConnection.on('ReceiveEventHistory', (history: string[]) => {
      console.log(`[Redis] Historial recibido: ${history.length} eventos`);
      const parsedHistory = history.map(h => {
        try { return JSON.parse(h); } catch { return null; }
      }).filter(h => h !== null);
      this.eventHistorySubject.next(parsedHistory);
    });

    this.hubConnection.onreconnecting((error) => {
      console.warn('Reconectando...', error);
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('Reconectado con ID:', connectionId);
    });

    this.hubConnection.onclose((error) => {
      console.log('Conexion cerrada', error);
    });
  }

  // Game Events
  private handleMessage(message: GameMessage): void {
    switch (message.type) {
      case 'PLAYER_MOVE':
        this.playerMoveSubject.next(message.data as PlayerPosition);
        break;

      case 'CHAT_MESSAGE':
        this.chatMessageSubject.next(message.data as ChatMessage);
        break;

      case 'PLAYER_JOINED':
        this.playerJoinedSubject.next(message.data as Player);
        break;

      case 'PLAYER_LEFT':
        this.playerLeftSubject.next(message.data.playerId);
        break;

      case 'CURRENT_PLAYERS':
        this.currentPlayersSubject.next(message.data as Player[]);
        break;

      case 'OBSTACLE_DESTROYED':
        this.obstacleDestroyedSubject.next(message.data as string);
        break;

      case 'PLAYER_READY':
        console.log('PLAYER_READY recibido - RAW:', JSON.stringify(message.data));
        const readyData = message.data;
        const playerReadyPayload = {
          playerId: readyData.playerId || readyData.PlayerId,
          isReady: readyData.isReady ?? readyData.IsReady
        };
        console.log('PLAYER_READY normalizado:', playerReadyPayload);
        this.playerReadySubject.next(playerReadyPayload);
        break;

      case 'GAME_STARTED':
        console.log('Juego iniciado!');
        this.gameStartedSubject.next(message.data as { roomId: string, players: Player[] });
        break;

      // Evento PLAYER_HIT con benchmarking de latencia
      case 'PLAYER_HIT':
        console.log('Jugador impactado:', message.data);
        const hitData = message.data as any;
        if (hitData.sentTimestamp) {
          this.benchmarkService.recordSample('PLAYER_HIT (SignalR)', hitData.sentTimestamp);
        }
        this.playerHitSubject.next({
          targetPlayerId: hitData.targetPlayerId || hitData.TargetPlayerId,
          shooterId: hitData.shooterId || hitData.ShooterId,
          damage: hitData.damage || hitData.Damage || 1
        });
        break;

      case 'TANK_SELECTED':
        console.log('Tanque seleccionado:', message.data);
        const tankData = message.data as any;
        this.tankSelectedSubject.next({
          playerId: tankData.playerId || tankData.PlayerId,
          tankType: tankData.tankType || tankData.TankType
        });
        break;

      case 'BULLET_FIRED':
        const bulletData = message.data as any;
        this.bulletFiredSubject.next({
          ownerId: bulletData.ownerId ?? bulletData.OwnerId,
          x: bulletData.x ?? bulletData.X,
          y: bulletData.y ?? bulletData.Y,
          rotation: bulletData.rotation ?? bulletData.Rotation
        });
        break;

      // Eventos de power-ups con benchmarking
      case 'POWER_UP_SPAWNED':
        console.log('[Lab06] Power-up aparecio:', message.data);
        const puData = message.data as any;
        if (puData.sentTimestamp) {
          this.benchmarkService.recordSample('POWER_UP_SPAWNED (SignalR)', puData.sentTimestamp);
        }
        this.powerUpSpawnedSubject.next({
          id: puData.id || puData.Id,
          x: puData.x || puData.X,
          y: puData.y || puData.Y,
          type: puData.type || puData.Type || 'health',
          sentTimestamp: puData.sentTimestamp
        });
        break;

      case 'COLLECT_POWER_UP':
        console.log('[Lab06] Power-up recolectado:', message.data);
        const collectData = message.data as any;
        if (collectData.sentTimestamp) {
          this.benchmarkService.recordSample('COLLECT_POWER_UP (SignalR)', collectData.sentTimestamp);
        }
        this.collectPowerUpSubject.next({
          powerUpId: collectData.powerUpId || collectData.PowerUpId,
          playerId: collectData.playerId || collectData.PlayerId,
          type: collectData.type || collectData.Type || 'health'
        });
        break;

      case 'GAME_OVER':
        console.log('Game Over recibido:', message.data);
        const goData = message.data as any;
        if (goData.sentTimestamp) {
          this.benchmarkService.recordSample('GAME_OVER (SignalR)', goData.sentTimestamp);
        }
        this.gameOverSubject.next({
          eliminatedPlayerId: goData.eliminatedPlayerId || goData.EliminatedPlayerId,
          eliminatedBy: goData.eliminatedBy || goData.EliminatedBy
        });
        break;

      case 'MATCH_ENDED':
        console.log('Match Ended:', message.data);
        const matchData = message.data as any;
        this.matchEndedSubject.next({
          roomId: matchData.roomId || matchData.RoomId,
          winnerId: matchData.winnerId || matchData.WinnerId,
          winnerName: matchData.winnerName || matchData.WinnerName,
          players: (matchData.players || matchData.Players || []).map((p: any) => ({
            playerId: p.playerId || p.PlayerId,
            playerName: p.playerName || p.PlayerName,
            isWinner: p.isWinner ?? p.IsWinner ?? false
          }))
        });
        break;

      default:
        console.log('Mensaje no reconocido:', message);
    }
  }

  private sendMessage(message: GameMessage): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection
        .invoke('SendMessage', message)
        .catch(err => console.error('Error enviando mensaje:', err));
    } else {
      console.error('No hay conexion activa al Hub');
    }
  }

  onPlayerMove() {
    return this.playerMoveSubject.asObservable();
  }

  sendPlayerMove(position: PlayerPosition): void {
    this.sendMessage({
      type: 'PLAYER_MOVE',
      data: position
    });
  }

  onChatMessage() {
    return this.chatMessageSubject.asObservable();
  }

  sendChatMessage(message: string, playerId: string, playerName: string): void {
    this.sendMessage({
      type: 'CHAT_MESSAGE',
      data: {
        playerId,
        playerName,
        message,
        timestamp: new Date()
      }
    });
  }

  onPlayerJoined() {
    return this.playerJoinedSubject.asObservable();
  }

  onPlayerLeft() {
    return this.playerLeftSubject.asObservable();
  }

  onCurrentPlayers() {
    return this.currentPlayersSubject.asObservable();
  }


  sendPlayerJoined(player: Player): void {
    console.log('Player joined automaticamente manejado por el Hub');
  }

  onObstacleDestroyed() {
    return this.obstacleDestroyedSubject.asObservable();
  }

  sendObstacleDestroyed(obstacleId: string): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection
        .invoke('DestroyObstacle', obstacleId)
        .catch(err => console.error('Error enviando destruccion:', err));
    }
  }

  onPlayerReady() {
    return this.playerReadySubject.asObservable();
  }

  sendPlayerReady(playerId: string, isReady: boolean): void {
    this.sendMessage({
      type: 'PLAYER_READY',
      data: { playerId, isReady }
    });
  }

  onGameStarted() {
    return this.gameStartedSubject.asObservable();
  }

  sendGameStart(roomId: string): void {
    this.sendMessage({
      type: 'START_GAME',
      data: { roomId }
    });
  }

  onPlayerHit() {
    return this.playerHitSubject.asObservable();
  }

  sendPlayerHit(targetPlayerId: string, shooterId: string, damage: number = 1): void {
    this.sendMessage({
      type: 'PLAYER_HIT',
      data: { targetPlayerId, shooterId, damage }
    });
  }

  onTankSelected() {
    return this.tankSelectedSubject.asObservable();
  }

  sendTankSelection(playerId: string, tankType: string): void {
    this.sendMessage({
      type: 'TANK_SELECTED',
      data: { playerId, tankType }
    });
  }

  onBulletFired() {
    return this.bulletFiredSubject.asObservable();
  }

  sendBulletFired(ownerId: string, x: number, y: number, rotation: number): void {
    this.sendMessage({
      type: 'BULLET_FIRED',
      data: { ownerId, x, y, rotation }
    });
  }

  // Metodos para power-ups
  onPowerUpSpawned() {
    return this.powerUpSpawnedSubject.asObservable();
  }

  sendPowerUpSpawned(powerUp: PowerUp): void {
    this.sendMessage({
      type: 'POWER_UP_SPAWNED',
      data: powerUp
    });
  }

  onCollectPowerUp() {
    return this.collectPowerUpSubject.asObservable();
  }

  sendCollectPowerUp(powerUpId: string, playerId: string, type: string): void {
    this.sendMessage({
      type: 'COLLECT_POWER_UP',
      data: { powerUpId, playerId, type }
    });
  }

  onGameOver() {
    return this.gameOverSubject.asObservable();
  }

  onMatchEnded() {
    return this.matchEndedSubject.asObservable();
  }

  sendGameOver(eliminatedPlayerId: string, eliminatedBy: string): void {
    this.sendMessage({
      type: 'GAME_OVER',
      data: { eliminatedPlayerId, eliminatedBy }
    });
  }

  onEventHistory() {
    return this.eventHistorySubject.asObservable();
  }

  acquire(): void {
    this.consumerCount++;
    console.log(`[GameService] Consumidor registrado. Total: ${this.consumerCount}`);
  }

  async relinquish(): Promise<void> {
    this.consumerCount = Math.max(0, this.consumerCount - 1);
    console.log(`[GameService] Consumidor liberado. Restantes: ${this.consumerCount}`);

    if (this.consumerCount === 0) {
      await this.disconnect();
    }
  }

  disconnect(): Promise<void> {
    if (this.hubConnection) {
      return this.hubConnection
        .stop()
        .then(() => {
          console.log('Desconectado del Hub');
          this.hubConnection = null;
        })
        .catch(err => {
          console.error('Error al desconectar:', err);
          throw err;
        });
    }
    return Promise.resolve();
  }

  isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  getConnectionState(): string {
    if (!this.hubConnection) return 'Disconnected';

    switch (this.hubConnection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected';
      default:
        return 'Unknown';
    }
  }
}
