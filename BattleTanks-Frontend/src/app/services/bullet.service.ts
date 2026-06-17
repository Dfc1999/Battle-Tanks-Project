import { Injectable, inject } from '@angular/core';
import { Bullet, GameConfig, DEFAULT_GAME_CONFIG } from '../models/game.models';
import { MapStore } from '../store/map.store';
import { PlayerStore } from '../store/player.store';

@Injectable({
    providedIn: 'root'
})
export class BulletService {
    private readonly mapStore = inject(MapStore);
    private readonly playerStore = inject(PlayerStore);
    private config: GameConfig = DEFAULT_GAME_CONFIG;

    setConfig(config: GameConfig): void {
        this.config = config;
    }

    createBullet(tankId: string, x: number, y: number, rotation: number): Bullet {
        const bullet: Bullet = {
            id: `bullet_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`,
            x,
            y,
            rotation,
            speed: this.config.bulletSpeed,
            ownerId: tankId
        };

        console.log(`[BulletService] Proyectil creado: ${bullet.id}`);
        return bullet;
    }

    updateBullets(bullets: Bullet[], tanks: Map<string, { id: string, x: number, y: number, width: number, height: number }> = new Map()): {
        activeBullets: Bullet[];
        destroyedObstacles: string[];
        hitObstacles: string[];
        hitPlayers: { targetId: string, shooterId: string }[];
    } {
        const activeBullets: Bullet[] = [];
        const destroyedObstacles: string[] = [];
        const hitObstacles: string[] = [];
        const hitPlayers: { targetId: string, shooterId: string }[] = [];
        const obstacles = this.mapStore.obstacles();

        for (const bullet of bullets) {
            const rad = (bullet.rotation * Math.PI) / 180;
            const newX = bullet.x + Math.sin(rad) * bullet.speed;
            const newY = bullet.y - Math.cos(rad) * bullet.speed;

            if (this.isOutOfBounds(newX, newY)) {
                continue;
            }

            const hitObstacle = this.checkObstacleCollision(newX, newY, obstacles);

            if (hitObstacle) {
                hitObstacles.push(hitObstacle.id);

                if (hitObstacle.destructible) {
                    destroyedObstacles.push(hitObstacle.id);
                    this.mapStore.destroyObstacle(hitObstacle.id);
                    this.playerStore.incrementScore(bullet.ownerId, 100);
                }
                continue;
            }

            const hitTank = this.checkTankCollision(newX, newY, tanks, bullet.ownerId);
            if (hitTank) {
                if (!bullet.isRemote) {
                    console.log(`Bala de ${bullet.ownerId} impactó a ${hitTank}`);
                    hitPlayers.push({ targetId: hitTank, shooterId: bullet.ownerId });
                    this.playerStore.incrementScore(bullet.ownerId, 200);
                }
                continue;
            }

            activeBullets.push({
                ...bullet,
                x: newX,
                y: newY
            });
        }

        return { activeBullets, destroyedObstacles, hitObstacles, hitPlayers };
    }

    private checkTankCollision(x: number, y: number, tanks: Map<string, { id: string, x: number, y: number, width: number, height: number }>, ownerId: string): string | null {
        for (const [tankId, tank] of tanks) {
            if (tankId === ownerId) continue;

            // Colisión AABB
            if (
                x > tank.x - tank.width / 2 &&
                x < tank.x + tank.width / 2 &&
                y > tank.y - tank.height / 2 &&
                y < tank.y + tank.height / 2
            ) {
                return tankId;
            }
        }
        return null;
    }

    private isOutOfBounds(x: number, y: number): boolean {
        return x < 0 || x > this.config.canvasWidth ||
            y < 0 || y > this.config.canvasHeight;
    }

    private checkObstacleCollision(x: number, y: number, obstacles: any[]): any | null {
        for (const obs of obstacles) {
            if (
                x > obs.x &&
                x < obs.x + obs.width &&
                y > obs.y &&
                y < obs.y + obs.height
            ) {
                return obs;
            }
        }
        return null;
    }

    getDirection(rotation: number): 'up' | 'down' | 'left' | 'right' {
        if (rotation === 0) return 'up';
        if (rotation === 90) return 'right';
        if (rotation === 180) return 'down';
        if (rotation === 270) return 'left';
        return 'up';
    }
}
