import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ElementRef, ViewChild, HostListener, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { GameService, PlayerPosition } from '../../services/game.service';
import { GameRendererService } from '../../services/game-renderer.service';
import { GamePhysicsService } from '../../services/game-physics.service';
import { BulletService } from '../../services/bullet.service';
import { AuthService } from '../../services/auth.service';
import { PlayerIdentityService } from '../../services/player-identity.service';
import { BenchmarkService, BenchmarkReport } from '../../services/benchmark.service';
import { RoomService } from '../../services/room.service';
import { PlayerStore, PlayerStatus } from '../../store/player.store';
import { MapStore, MapData, TileType } from '../../store/map.store';
import { Tank, Bullet, Obstacle, DEFAULT_GAME_CONFIG, TANK_TYPES, PowerUp, PowerUpType } from '../../models/game.models';

export enum GamePhase {
  Waiting    = 'Waiting',
  InProgress = 'InProgress',
  Finished   = 'Finished',
}

@Component({
  selector: 'app-game-canvas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './game-canvas.component.html',
  styleUrls: ['./game-canvas.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GameCanvasComponent implements OnInit, OnDestroy {
  @ViewChild('gameCanvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  // Conectar UI con NgRx Store
  readonly store = inject(PlayerStore);
  readonly mapStore = inject(MapStore);
  private readonly renderer = inject(GameRendererService);
  private readonly physics = inject(GamePhysicsService);
  private readonly bulletService = inject(BulletService);
  private readonly gameService = inject(GameService);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly benchmarkService = inject(BenchmarkService);
  private readonly router = inject(Router);
  private readonly playerIdentity = inject(PlayerIdentityService);
  private readonly roomService = inject(RoomService);
  private readonly gamePhase = signal<GamePhase>(GamePhase.Waiting);

  private tanks: Map<string, Tank> = new Map();
  private bullets: Bullet[] = [];
  private keys: Set<string> = new Set();

  // Power-ups
  private powerUps: PowerUp[] = [];
  private powerUpSpawnInterval: any = null;
  private readonly POWER_UP_SPAWN_INTERVAL = 45000;
  private readonly MAX_POWER_UPS = 5;
  private readonly POWER_UP_COLLECT_DISTANCE = 30;

  // Notificaciones de eventos criticos
  gameNotifications: { message: string, type: string, timestamp: number }[] = [];

  private currentPlayerId = '';
  private roomId = '';
  playerName = '';

  private animationFrameId = 0;
  private subscriptions: Subscription[] = [];

  matchResult: {
    roomId: string;
    winnerId: string;
    winnerName: string;
    players: { playerId: string; playerName: string; isWinner: boolean }[];
  } | null = null;

  get activePlayers(): number {
    return this.tanks.size;
  }

  get isConnected(): boolean {
    return this.gameService.isConnected();
  }

  get playerStatus(): PlayerStatus {
    return this.store.currentPlayerStatus();
  }

  get playerScore(): number {
    return this.store.currentPlayerScore();
  }

  get playerAmmo(): number {
    return this.store.currentPlayerAmmo();
  }

  get playerLives(): boolean[] {
    return this.store.livesArray();
  }

  get activePowerUps(): number {
    return this.powerUps.length;
  }

  get benchmarkSamples(): number {
    return this.benchmarkService.getSampleCount();
  }

  // Panel de benchmark
  showBenchmarkPanel = false;
  benchmarkReports: BenchmarkReport[] = [];

  get statusLabel(): string {
    const status = this.store.currentPlayerStatus();
    const labels: Record<PlayerStatus, string> = {
      alive: 'Vivo',
      wounded: 'Herido',
      dead: 'Muerto'
    };
    return labels[status];
  }

  async ngOnInit(): Promise<void> {
    this.route.queryParams.subscribe(params => {
      this.roomId = params['roomId'] || 'default-room';
      console.log('[GameCanvas] Sala del juego:', this.roomId);
    });

    this.initializeGame();
    await this.connectToServer();

    this.gamePhase.set(GamePhase.InProgress);

    this.startGameLoop();
    this.startPowerUpSpawner();
  }

  ngOnDestroy(): void {
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
    }
    if (this.powerUpSpawnInterval) {
      clearInterval(this.powerUpSpawnInterval);
    }
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.gameService.relinquish();      
  }


  private initializeGame(): void {
    this.renderer.initialize(this.canvasRef.nativeElement);
    this.physics.setConfig(this.renderer.getConfig());
    this.bulletService.setConfig(this.renderer.getConfig());

    const identity      = this.playerIdentity.getIdentityOrThrow();
    this.currentPlayerId = identity.dbId;
    this.playerName      = identity.username;

    console.log(`Jugador: ${this.playerName} (${this.currentPlayerId})`);

    this.store.addPlayer({
      id: this.currentPlayerId,
      name: this.playerName,
      color: '#4CAF50'
    });
    this.store.setCurrentPlayerId(this.currentPlayerId);

    this.loadMap();

    this.renderer.loadSprites().then(() => {
      console.log('Sprites de tanques cargados');
    }).catch(err => {
      console.error('Error cargando sprites:', err);
    });

    const currentPlayerState = this.store.currentPlayer();
    const tankType = currentPlayerState?.tankType || 'e100';

    const playerTank = this.physics.createPlayerTank(this.currentPlayerId, tankType);
    this.tanks.set(this.currentPlayerId, playerTank);

    this.setupNetworkListeners();
  }

  private loadMap(): void {
    this.http.get<MapData>('assets/data/map.json').subscribe({
      next: (mapData) => {
        if (mapData.tiles?.length > 0) {
          this.mapStore.loadMap(mapData);
          console.log(`[GameCanvas] Mapa "${mapData.name}" cargado: ${this.mapStore.obstacles().length} obstáculos`);
        } else {
          console.warn('[GameCanvas] map.json está vacío — cargando mapa de respaldo');
          this.loadFallbackMap();
        }
      },
      error: (err) => {
        console.error('[GameCanvas] Error al cargar map.json:', err);
        this.loadFallbackMap();
      }
    });
  }

  private loadFallbackMap(): void {
    const obstacles  = this.physics.createObstacles();
    const config     = this.renderer.getConfig();

    const fallbackMap: MapData = {
      name:        'Default Arena',
      description: 'Mapa de respaldo generado por GamePhysicsService',
      tileSize:    config.tileSize,
      legend:      { '0': 'empty', '1': 'indestructible', '2': 'destructible' },
      tiles:       this.buildTileGrid(obstacles, config)
    };

    this.mapStore.loadMap(fallbackMap);
    console.log(`[GameCanvas] Mapa de respaldo cargado: ${this.mapStore.obstacles().length} obstáculos`);
  }

  private buildTileGrid(
    obstacles: Obstacle[],
    config: { canvasWidth: number; canvasHeight: number; tileSize: number }
  ): TileType[][] {
    const rows = Math.ceil(config.canvasHeight / config.tileSize);
    const cols = Math.ceil(config.canvasWidth  / config.tileSize);

    const tiles: TileType[][] = Array.from(
      { length: rows },
      () => new Array<TileType>(cols).fill(0)
    );

    for (const obs of obstacles) {
      const col = Math.floor(obs.x / config.tileSize);
      const row = Math.floor(obs.y / config.tileSize);

      if (row >= 0 && row < rows && col >= 0 && col < cols) {
        tiles[row][col] = obs.destructible ? 2 : 1;
      }
    }

    return tiles;
  }

  private async connectToServer(): Promise<void> {
    this.gameService.acquire();         

    if (!this.gameService.isConnected()) {
      try {
        const roomToConnect = this.roomId || 'default-room';
        await this.gameService.connect(roomToConnect, this.currentPlayerId, this.playerName);
        console.log(`[GameCanvas] Conectado al servidor en sala: ${roomToConnect}`);

        // Broadcast initial position multiple times so late-joining players see this tank
        this.broadcastInitialPosition();
      } catch (error) {
        console.error('[GameCanvas] Error de conexión:', error);
        this.gameService.relinquish();  
      }
    }
  }

  /**
   * Sends the player's position several times over the first few seconds
   * so that other players who connect slightly later will still receive it.
   */
  private broadcastInitialPosition(): void {
    const playerTank = this.tanks.get(this.currentPlayerId);
    if (!playerTank) return;

    // Send immediately
    this.sendPositionToServer(playerTank);

    // Then resend at 1s, 2s, and 3s to catch late joiners
    let sendCount = 0;
    const intervalId = setInterval(() => {
      sendCount++;
      const tank = this.tanks.get(this.currentPlayerId);
      if (tank && this.gameService.isConnected()) {
        this.sendPositionToServer(tank);
      }
      if (sendCount >= 3) {
        clearInterval(intervalId);
      }
    }, 1000);
  }

  private setupNetworkListeners(): void {
    const moveSub = this.gameService.onPlayerMove().subscribe({
      next: (position) => this.handleRemotePlayerMove(position)
    });

    const obstacleSub = this.gameService.onObstacleDestroyed().subscribe({
      next: (obstacleId) => {
        this.mapStore.destroyObstacle(obstacleId);
      }
    });

    const playerHitSub = this.gameService.onPlayerHit().subscribe({
      next: (data) => {
        console.log(`Impacto recibido: ${data.shooterId} -> ${data.targetPlayerId}`);
        this.store.takeDamage(data.targetPlayerId, data.damage);

        if (data.targetPlayerId === this.currentPlayerId) {
          this.addNotification('💥 Te han disparado!', 'danger');

          // Verificar si el jugador murio para enviar GAME_OVER
          if (this.store.currentPlayerStatus() === 'dead') {
            this.gameService.sendGameOver(this.currentPlayerId, data.shooterId);
          }
        } else {
          this.addNotification(`🎯 Impacto en enemigo!`, 'success');
        }
      }
    });

    const bulletFiredSub = this.gameService.onBulletFired().subscribe({
      next: (data) => {
        if (data.ownerId === this.currentPlayerId) return;
        const bullet = this.bulletService.createBullet(
          data.ownerId,
          data.x,
          data.y,
          data.rotation
        );
        bullet.isRemote = true;
        this.bullets.push(bullet);
      }
    });

    // Suscripcion a eventos de power-ups
    const powerUpSub = this.gameService.onPowerUpSpawned().subscribe({
      next: (powerUp) => {
        console.log('[Lab06] Power-up recibido via SignalR:', powerUp);
        const exists = this.powerUps.find(p => p.id === powerUp.id);
        if (!exists) {
          this.powerUps.push(powerUp);
        }
      }
    });

    const collectPowerUpSub = this.gameService.onCollectPowerUp().subscribe({
      next: (data) => {
        console.log('[Lab06] Power-up recolectado:', data.powerUpId);
        this.powerUps = this.powerUps.filter(p => p.id !== data.powerUpId);

        // Aplicar efecto del power-up
        if (data.playerId === this.currentPlayerId) {
          this.applyPowerUpEffect(data.type as PowerUpType);
          const typeLabel = data.type === 'health' ? '❤️ Vida' : data.type === 'ammo' ? '🔫 Munición' : '⚡ Velocidad';
          this.addNotification(`${typeLabel} recogido!`, 'info');
        }
      }
    });

    this.subscriptions.push(moveSub, obstacleSub, playerHitSub, bulletFiredSub, powerUpSub, collectPowerUpSub);

    // Suscripcion a evento GAME_OVER
    const gameOverSub = this.gameService.onGameOver().subscribe({
      next: (data) => {
        if (data.eliminatedPlayerId === this.currentPlayerId) {
          this.addNotification('☠️ Has sido eliminado!', 'danger');
        } else {
          this.addNotification(`🏆 Enemigo eliminado!`, 'success');
          this.store.incrementScore(this.currentPlayerId, 200);
        }
      }
    });

    const matchEndedSub = this.gameService.onMatchEnded().subscribe({
      next: (data) => {
        console.log('[GameCanvas] MATCH_ENDED recibido:', data);

        this.gamePhase.set(GamePhase.Finished);

        this.matchResult = data;

        if (this.animationFrameId) {
          cancelAnimationFrame(this.animationFrameId);
          this.animationFrameId = 0;
        }

        if (this.powerUpSpawnInterval) {
          clearInterval(this.powerUpSpawnInterval);
          this.powerUpSpawnInterval = null;
        }

        this.persistScore(data.roomId);
      }
    });

    this.subscriptions.push(gameOverSub, matchEndedSub);

    // Suscripcion a historial de eventos de Redis
    const historySub = this.gameService.onEventHistory().subscribe({
      next: (events) => {
        console.log(`[Redis] Historial de sala recibido: ${events.length} eventos`);
        if (events.length > 0) {
          this.addNotification(`📋 ${events.length} eventos previos cargados`, 'info');
        }
      }
    });

    this.subscriptions.push(historySub);
  }

  // Spawner periodico de power-ups
  private startPowerUpSpawner(): void {
    this.powerUpSpawnInterval = setInterval(() => {
      const canSpawn =
        this.gamePhase() === GamePhase.InProgress &&
        this.gameService.isConnected() &&
        this.store.currentPlayerStatus() !== 'dead' &&
        this.powerUps.length < this.MAX_POWER_UPS;

      if (canSpawn) {
        this.spawnPowerUp();
      }
    }, this.POWER_UP_SPAWN_INTERVAL);
  }

  private spawnPowerUp(): void {
    const config = this.renderer.getConfig();
    const types: PowerUpType[] = ['health', 'ammo', 'speed'];
    const type = types[Math.floor(Math.random() * types.length)];

    const powerUp: PowerUp = {
      id: `pu_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`,
      x: 50 + Math.random() * (config.canvasWidth - 100),
      y: 50 + Math.random() * (config.canvasHeight - 100),
      type
    };

    console.log(`[Lab06] Generando power-up: ${type} en (${Math.round(powerUp.x)}, ${Math.round(powerUp.y)})`);
    this.gameService.sendPowerUpSpawned(powerUp);
  }

  private checkPowerUpCollision(): void {
    const playerTank = this.tanks.get(this.currentPlayerId);
    if (!playerTank) return;

    for (const powerUp of [...this.powerUps]) {
      const dx = playerTank.x - powerUp.x;
      const dy = playerTank.y - powerUp.y;
      const distance = Math.sqrt(dx * dx + dy * dy);

      if (distance < this.POWER_UP_COLLECT_DISTANCE) {
        console.log(`[Lab06] Recolectando power-up: ${powerUp.type}`);
        this.gameService.sendCollectPowerUp(powerUp.id, this.currentPlayerId, powerUp.type);
      }
    }
  }

  private applyPowerUpEffect(type: PowerUpType): void {
    switch (type) {
      case 'health':
        this.store.heal(this.currentPlayerId, 1);
        console.log('[Lab06] +1 vida!');
        break;
      case 'ammo':
        this.store.reloadAmmo(this.currentPlayerId);
        console.log('[Lab06] Municion recargada!');
        break;
      case 'speed':
        const tank = this.tanks.get(this.currentPlayerId);
        if (tank) {
          tank.speed = DEFAULT_GAME_CONFIG.tankSpeed * 1.5;
          setTimeout(() => { tank.speed = DEFAULT_GAME_CONFIG.tankSpeed; }, 5000);
          console.log('[Lab06] Velocidad aumentada por 5 segundos!');
        }
        break;
    }
    this.store.incrementScore(this.currentPlayerId, 50);
  }

  private startGameLoop(): void {
    const loop = () => {
      this.update();
      this.render();
      this.animationFrameId = requestAnimationFrame(loop);
    };
    loop();
  }

  private update(): void {
    if (this.store.currentPlayerStatus() === 'dead') return;

    const playerTank = this.tanks.get(this.currentPlayerId);
    if (!playerTank) return;

    const movement = this.physics.calculateTankMovement(
      playerTank, this.keys, this.mapStore.obstacles(), Array.from(this.tanks.values())
    );

    // Eventos de movimiento
    if (movement.moved) {
      playerTank.x = movement.newX;
      playerTank.y = movement.newY;
      playerTank.rotation = movement.newRotation;

      // Sincronizar posicion en SignalStore
      this.store.updatePlayerPosition({
        playerId: this.currentPlayerId,
        x: movement.newX,
        y: movement.newY,
        rotation: movement.newRotation
      });

      this.sendPositionToServer(playerTank);
    }

    this.updateBullets();

    // Verificar colision con power-ups
    this.checkPowerUpCollision();
  }

  private updateBullets(): void {
    const result = this.bulletService.updateBullets(this.bullets, this.tanks);

    this.bullets = result.activeBullets;

    result.destroyedObstacles.forEach((obstacleId) => {
      console.log('Obstaculo destruido! +100 puntos');
      this.gameService.sendObstacleDestroyed(obstacleId);
    });

    result.hitPlayers.forEach((hit) => {
      console.log(`Enviando impacto: ${hit.shooterId} -> ${hit.targetId}`);
      this.gameService.sendPlayerHit(hit.targetId, hit.shooterId, 1);
    });

    this.mapStore.updateExplosions();
  }

  private render(): void {
    this.renderer.clearCanvas();
    this.renderer.drawGrid();
    this.renderer.drawObstacles(this.mapStore.obstacles());
    this.renderer.drawBullets(this.bullets);
    this.renderer.drawExplosions(this.mapStore.explosions());

    // Dibujar power-ups en el canvas
    this.drawPowerUps();

    this.tanks.forEach(tank => {
      const isCurrentPlayer = tank.id === this.currentPlayerId;
      const status = isCurrentPlayer ? this.store.currentPlayerStatus() : 'alive';
      const health = isCurrentPlayer ? this.store.currentPlayerLives() : 3;
      const maxHealth = 3;
      this.renderer.drawTank(tank, isCurrentPlayer, status, health, maxHealth);
    });

    if (this.store.currentPlayerStatus() === 'dead') {
      this.renderer.drawGameOver(this.store.currentPlayerScore());
    }
  }

  // Renderizado de power-ups
  private drawPowerUps(): void {
    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    for (const powerUp of this.powerUps) {
      ctx.save();

      const pulse = Math.sin(Date.now() / 300) * 0.15 + 0.85;
      const size = 16 * pulse;

      let color = '#4CAF50';
      let emoji = '❤️';
      if (powerUp.type === 'ammo') {
        color = 'rgba(174, 255, 0, 1)';
        emoji = '🔫';
      } else if (powerUp.type === 'speed') {
        color = '#2196F3';
        emoji = '⚡';
      }

      ctx.shadowColor = color;
      ctx.shadowBlur = 15;
      ctx.fillStyle = color;
      ctx.beginPath();
      ctx.arc(powerUp.x, powerUp.y, size, 0, Math.PI * 2);
      ctx.fill();

      ctx.shadowBlur = 0;
      ctx.strokeStyle = '#fff';
      ctx.lineWidth = 2;
      ctx.stroke();

      ctx.font = '14px Arial';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(emoji, powerUp.x, powerUp.y);

      ctx.restore();
    }
  }

  private handleRemotePlayerMove(position: PlayerPosition): void {
    if (position.playerId === this.currentPlayerId) return;

    let tank = this.tanks.get(position.playerId);
    if (!tank) {
      const playerState = this.store.players().find(p => p.id === position.playerId);
      const remoteTankType = playerState?.tankType || 'e100';

      tank = {
        id: position.playerId,
        x: position.x,
        y: position.y,
        rotation: position.rotation,
        color: this.physics.getRandomColor(),
        width: 36,
        height: 36,
        speed: DEFAULT_GAME_CONFIG.tankSpeed,
        tankType: remoteTankType
      };
      this.tanks.set(position.playerId, tank);
    } else {
      tank.x = position.x;
      tank.y = position.y;
      tank.rotation = position.rotation;
    }
  }

  private sendPositionToServer(tank: Tank): void {
    if (this.gameService.isConnected()) {
      this.gameService.sendPlayerMove({
        playerId: tank.id,
        x: tank.x,
        y: tank.y,
        rotation: tank.rotation
      });
    }
  }

  private shoot(): void {
    if (this.store.currentPlayerStatus() === 'dead') return;
    if (!this.store.decrementAmmo(this.currentPlayerId)) {
      console.log('Sin municiones');
      return;
    }

    const playerTank = this.tanks.get(this.currentPlayerId);
    if (playerTank) {
      const bullet = this.bulletService.createBullet(
        this.currentPlayerId,
        playerTank.x,
        playerTank.y,
        playerTank.rotation
      );
      this.bullets.push(bullet);

      this.gameService.sendBulletFired(
        this.currentPlayerId,
        playerTank.x,
        playerTank.y,
        playerTank.rotation
      );

      console.log('Disparo!');
    }
  }

  simulateDamage(): void {
    this.store.takeDamage(this.currentPlayerId);
  }

  reload(): void {
    this.store.reloadAmmo(this.currentPlayerId);
    console.log('Municiones recargadas');
  }

  restart(): void {
    this.store.resetPlayer(this.currentPlayerId);

    const playerTank = this.tanks.get(this.currentPlayerId);
    if (playerTank) {
      const newTank = this.physics.createPlayerTank(this.currentPlayerId, playerTank.tankType);
      playerTank.x = newTank.x;
      playerTank.y = newTank.y;
      playerTank.rotation = newTank.rotation;
    }

    this.mapStore.resetMap();
    this.bullets = [];
    this.powerUps = [];
    console.log('Juego reiniciado');
  }

  private persistScore(roomId: string): void {
    const score = this.store.currentPlayerScore();
    if (score <= 0) return;

    if (!this.playerIdentity.hasValidIdentity()) {
      console.warn('[GameCanvas] No se puede persistir el score: sin identidad válida.');
      return;
    }

    const { dbId } = this.playerIdentity.getIdentityOrThrow();

    this.http.post('http://localhost:5013/api/scores', {
      playerId:  dbId,    
      sessionId: roomId,
      points:    score
    }).subscribe({
      next: ()    => console.log('[GameCanvas] Score persistido:', score),
      error: (err) => console.error('[GameCanvas] Error al persistir score:', err)
    });
  }

  backToLobby(): void {
    this.matchResult = null;
    this.gameService.relinquish();

    // Delete/leave the room so a new one can be created
    if (this.roomId && this.roomId !== 'default-room') {
      this.roomService.deleteRoom(this.roomId).subscribe({
        next: () => console.log('[GameCanvas] Sala eliminada tras partida'),
        error: () => {
          // If delete fails (e.g. no DELETE endpoint), try leaving instead
          this.roomService.leaveRoom(this.roomId).subscribe({
            next: () => console.log('[GameCanvas] Sala abandonada tras partida'),
            error: (err) => console.error('[GameCanvas] Error limpiando sala:', err)
          });
        }
      });
    }

    this.router.navigate(['/menu']);
  }

  // Mostrar reporte de benchmark
  showBenchmark(): void {
    this.benchmarkService.printReport();
    this.benchmarkReports = this.benchmarkService.getReport();
    this.showBenchmarkPanel = !this.showBenchmarkPanel;
  }

  // Notificaciones de eventos criticos
  addNotification(message: string, type: string): void {
    const notification = { message, type, timestamp: Date.now() };
    this.gameNotifications.push(notification);
    setTimeout(() => {
      this.gameNotifications = this.gameNotifications.filter(n => n !== notification);
    }, 3000);
    if (this.gameNotifications.length > 5) {
      this.gameNotifications.shift();
    }
  }

  closeBenchmarkPanel(): void {
    this.showBenchmarkPanel = false;
  }

  @HostListener('window:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    const key = event.key.toLowerCase();

    if (['w', 'a', 's', 'd', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright'].includes(key)) {
      event.preventDefault();
      this.keys.add(key);
    }

    if (key === ' ' || key === 'space') {
      event.preventDefault();
      this.shoot();
    }

    if (key === 'r') {
      this.store.currentPlayerStatus() === 'dead' ? this.restart() : this.reload();
    }

    if (key === 'b') {
      this.showBenchmark();
    }
  }

  @HostListener('window:keyup', ['$event'])
  onKeyUp(event: KeyboardEvent): void {
    this.keys.delete(event.key.toLowerCase());
  }
}
