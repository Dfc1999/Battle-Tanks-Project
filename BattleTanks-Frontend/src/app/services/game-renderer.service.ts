import { Injectable } from '@angular/core';
import { Tank, Bullet, Obstacle, GameConfig, DEFAULT_GAME_CONFIG, Explosion, TANK_TYPES } from '../models/game.models';

@Injectable({
    providedIn: 'root'
})
export class GameRendererService {
    private ctx!: CanvasRenderingContext2D;
    private config: GameConfig = DEFAULT_GAME_CONFIG;
    private spriteCache: Map<string, HTMLImageElement> = new Map();
    private spritesLoaded = false;

    initialize(canvas: HTMLCanvasElement, config?: Partial<GameConfig>): void {
        if (config) {
            this.config = { ...this.config, ...config };
        }

        canvas.width = this.config.canvasWidth;
        canvas.height = this.config.canvasHeight;

        const context = canvas.getContext('2d');
        if (!context) {
            throw new Error('No se pudo obtener el contexto 2D del canvas');
        }
        this.ctx = context;
    }

    loadSprites(): Promise<void> {
        const promises = TANK_TYPES.map(tankType => {
            return new Promise<void>((resolve, reject) => {
                if (this.spriteCache.has(tankType.id)) {
                    resolve();
                    return;
                }
                const img = new Image();
                img.onload = () => {
                    this.spriteCache.set(tankType.id, img);
                    console.log(`Sprite cargado: ${tankType.name}`);
                    resolve();
                };
                img.onerror = (err) => {
                    console.error(`Error cargando sprite ${tankType.name}:`, err);
                    reject(err);
                };
                img.src = tankType.spriteUrl;
            });
        });

        return Promise.all(promises).then(() => {
            this.spritesLoaded = true;
            console.log('Todos los sprites cargados');
        });
    }

    clearCanvas(): void {
        this.ctx.fillStyle = '#1a2f1a';
        this.ctx.fillRect(0, 0, this.config.canvasWidth, this.config.canvasHeight);
    }

    drawGrid(): void {
        this.ctx.strokeStyle = '#2a3f2a';
        this.ctx.lineWidth = 1;

        for (let x = 0; x <= this.config.canvasWidth; x += this.config.tileSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(x, 0);
            this.ctx.lineTo(x, this.config.canvasHeight);
            this.ctx.stroke();
        }

        for (let y = 0; y <= this.config.canvasHeight; y += this.config.tileSize) {
            this.ctx.beginPath();
            this.ctx.moveTo(0, y);
            this.ctx.lineTo(this.config.canvasWidth, y);
            this.ctx.stroke();
        }
    }

    drawTank(tank: Tank, isCurrentPlayer: boolean = false, playerStatus: 'alive' | 'wounded' | 'dead' = 'alive', health: number = 3, maxHealth: number = 3): void {
        this.ctx.save();
        this.ctx.translate(tank.x, tank.y);
        this.ctx.rotate((tank.rotation * Math.PI) / 180 + Math.PI);

        const sprite = this.spriteCache.get(tank.tankType || 'e100');

        if (sprite && this.spritesLoaded) {
            if (playerStatus === 'dead') {
                this.ctx.globalAlpha = 0.4;
            } else if (playerStatus === 'wounded') {
                this.ctx.globalAlpha = Math.floor(Date.now() / 200) % 2 === 0 ? 0.6 : 1.0;
            }

            this.ctx.shadowColor = 'rgba(0, 0, 0, 0.5)';
            this.ctx.shadowBlur = 8;
            this.ctx.shadowOffsetX = 3;
            this.ctx.shadowOffsetY = 3;

            const spriteW = tank.width + 8;
            const spriteH = tank.height + 8;
            this.ctx.drawImage(sprite, -spriteW / 2, -spriteH / 2, spriteW, spriteH);

            this.ctx.shadowColor = 'transparent';
            this.ctx.shadowBlur = 0;
            this.ctx.shadowOffsetX = 0;
            this.ctx.shadowOffsetY = 0;

            this.ctx.globalAlpha = 1.0;
        } else {
            this.ctx.fillStyle = tank.color;
            this.ctx.fillRect(-tank.width / 2, -tank.height / 2, tank.width, tank.height);
            this.ctx.strokeStyle = '#000';
            this.ctx.lineWidth = 2;
            this.ctx.strokeRect(-tank.width / 2, -tank.height / 2, tank.width, tank.height);

            this.ctx.fillStyle = this.darkenColor(tank.color);
            this.ctx.beginPath();
            this.ctx.arc(0, 0, 10, 0, Math.PI * 2);
            this.ctx.fill();

            this.ctx.fillStyle = '#222';
            this.ctx.fillRect(-3, -tank.height / 2 - 12, 6, 16);
        }

        if (isCurrentPlayer && playerStatus !== 'dead') {
            const pulse = Math.sin(Date.now() / 300) * 0.3 + 0.7;
            const glowColor = playerStatus === 'wounded'
                ? `rgba(255, 152, 0, ${pulse * 0.6})`
                : `rgba(0, 200, 255, ${pulse * 0.5})`;

            this.ctx.strokeStyle = glowColor;
            this.ctx.lineWidth = 2.5;
            this.ctx.beginPath();
            this.ctx.arc(0, 0, 26, 0, Math.PI * 2);
            this.ctx.stroke();

            this.ctx.fillStyle = glowColor;
            this.ctx.beginPath();
            this.ctx.moveTo(0, -32);
            this.ctx.lineTo(-5, -38);
            this.ctx.lineTo(5, -38);
            this.ctx.closePath();
            this.ctx.fill();
        }

        this.ctx.restore();
    }

    drawObstacles(obstacles: Obstacle[]): void {
        for (const obs of obstacles) {
            this.ctx.fillStyle = obs.color;
            this.ctx.fillRect(obs.x, obs.y, obs.width, obs.height);

            this.ctx.strokeStyle = obs.destructible ? '#5a3a0a' : '#4a5a6a';
            this.ctx.lineWidth = 2;
            this.ctx.strokeRect(obs.x, obs.y, obs.width, obs.height);

            if (obs.destructible) {
                this.ctx.strokeStyle = '#6a4a1a';
                this.ctx.lineWidth = 1;
                this.ctx.beginPath();
                this.ctx.moveTo(obs.x, obs.y + obs.height / 2);
                this.ctx.lineTo(obs.x + obs.width, obs.y + obs.height / 2);
                this.ctx.stroke();
                this.ctx.beginPath();
                this.ctx.moveTo(obs.x + obs.width / 2, obs.y);
                this.ctx.lineTo(obs.x + obs.width / 2, obs.y + obs.height / 2);
                this.ctx.stroke();
            } else {
                this.ctx.fillStyle = '#8a9aaa';
                this.ctx.beginPath();
                this.ctx.arc(obs.x + obs.width / 2, obs.y + obs.height / 2, 5, 0, Math.PI * 2);
                this.ctx.fill();
            }
        }
    }

    drawBullets(bullets: Bullet[]): void {
        for (const bullet of bullets) {
            this.ctx.fillStyle = '#FFD700';
            this.ctx.beginPath();
            this.ctx.arc(bullet.x, bullet.y, 4, 0, Math.PI * 2);
            this.ctx.fill();

            this.ctx.fillStyle = '#FFF';
            this.ctx.beginPath();
            this.ctx.arc(bullet.x - 1, bullet.y - 1, 1.5, 0, Math.PI * 2);
            this.ctx.fill();
        }
    }

    drawGameOver(score: number): void {
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
        this.ctx.fillRect(0, 0, this.config.canvasWidth, this.config.canvasHeight);

        this.ctx.fillStyle = '#FF0000';
        this.ctx.font = 'bold 48px "Press Start 2P", monospace';
        this.ctx.textAlign = 'center';
        this.ctx.fillText('GAME OVER', this.config.canvasWidth / 2, this.config.canvasHeight / 2);

        this.ctx.fillStyle = '#FFF';
        this.ctx.font = '20px monospace';
        this.ctx.fillText(`Puntuación: ${score}`, this.config.canvasWidth / 2, this.config.canvasHeight / 2 + 50);
        this.ctx.fillText('Presiona R para reiniciar', this.config.canvasWidth / 2, this.config.canvasHeight / 2 + 90);
    }

    private darkenColor(color: string): string {
        const num = parseInt(color.replace('#', ''), 16);
        const r = Math.max(0, (num >> 16) - 40);
        const g = Math.max(0, ((num >> 8) & 0x00FF) - 40);
        const b = Math.max(0, (num & 0x0000FF) - 40);
        return `#${((r << 16) | (g << 8) | b).toString(16).padStart(6, '0')}`;
    }

    getConfig(): GameConfig {
        return this.config;
    }

    drawExplosions(explosions: Explosion[]): void {
        for (const explosion of explosions) {
            const progress = explosion.frame / explosion.maxFrames;
            const radius = 20 + (progress * 30);
            const alpha = 1 - progress;

            this.ctx.fillStyle = `rgba(255, 165, 0, ${alpha * 0.8})`;
            this.ctx.beginPath();
            this.ctx.arc(explosion.x, explosion.y, radius, 0, Math.PI * 2);
            this.ctx.fill();

            this.ctx.fillStyle = `rgba(255, 69, 0, ${alpha})`;
            this.ctx.beginPath();
            this.ctx.arc(explosion.x, explosion.y, radius * 0.7, 0, Math.PI * 2);
            this.ctx.fill();

            this.ctx.fillStyle = `rgba(255, 255, 0, ${alpha})`;
            this.ctx.beginPath();
            this.ctx.arc(explosion.x, explosion.y, radius * 0.4, 0, Math.PI * 2);
            this.ctx.fill();

            if (explosion.frame < explosion.maxFrames / 2) {
                this.ctx.fillStyle = `rgba(255, 200, 50, ${alpha})`;
                for (let i = 0; i < 6; i++) {
                    const angle = (i / 6) * Math.PI * 2 + (explosion.frame * 0.2);
                    const dist = radius * 0.8 + (Math.random() * 10);
                    const px = explosion.x + Math.cos(angle) * dist;
                    const py = explosion.y + Math.sin(angle) * dist;
                    this.ctx.fillRect(px - 3, py - 3, 6, 6);
                }
            }
        }
    }
}
