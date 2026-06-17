import { Injectable } from '@angular/core';
import { Tank, Bullet, Obstacle, GameConfig, DEFAULT_GAME_CONFIG } from '../models/game.models';

@Injectable({
    providedIn: 'root'
})
export class GamePhysicsService {
    private config: GameConfig = DEFAULT_GAME_CONFIG;

    setConfig(config: GameConfig): void {
        this.config = config;
    }

    createObstacles(): Obstacle[] {
        const obstacles: Obstacle[] = [];

        const brickPositions = [
            { x: 200, y: 150 }, { x: 240, y: 150 }, { x: 280, y: 150 },
            { x: 500, y: 200 }, { x: 540, y: 200 }, { x: 580, y: 200 },
            { x: 300, y: 350 }, { x: 340, y: 350 }, { x: 380, y: 350 },
            { x: 150, y: 400 }, { x: 600, y: 450 },
        ];

        brickPositions.forEach((pos, i) => {
            obstacles.push({
                id: `brick_${i}`,
                x: pos.x,
                y: pos.y,
                width: this.config.tileSize,
                height: this.config.tileSize,
                destructible: true,
                health: 1,
                color: '#8B4513'
            });
        });

        const metalPositions = [
            { x: 100, y: 100 }, { x: 700, y: 100 },
            { x: 100, y: 500 }, { x: 700, y: 500 },
            { x: 400, y: 280 },
        ];

        metalPositions.forEach((pos, i) => {
            obstacles.push({
                id: `metal_${i}`,
                x: pos.x,
                y: pos.y,
                width: this.config.tileSize,
                height: this.config.tileSize,
                destructible: false,
                health: -1,
                color: '#708090'
            });
        });

        return obstacles;
    }

    // Spawn positions verified against map.json (tile coordinates mapped to pixel centers)
    // These are all on empty tiles (type 0) at the 4 inside corners of the map
    // Map: 20 cols × 15 rows, tileSize=40, borders are indestructible (row 0/14, col 0/19)
    // Tile(col,row) → pixel center: (col*40 + 20, row*40 + 20)
    private readonly SPAWN_POSITIONS = [
        { x: 60,  y: 60,  rotation: 180 },  // Tile(1,1) = empty → top-left corner
        { x: 740, y: 540, rotation: 0 },    // Tile(18,13) = empty → bottom-right corner
        { x: 740, y: 60,  rotation: 270 },  // Tile(18,1) = empty → top-right corner
        { x: 60,  y: 540, rotation: 90 },   // Tile(1,13) = empty → bottom-left corner
    ];

    /**
     * Deterministic hash of playerId to a spawn index.
     * Same playerId always gets the same spawn position on every client.
     */
    private hashPlayerIdToIndex(playerId: string): number {
        let hash = 0;
        for (let i = 0; i < playerId.length; i++) {
            hash = ((hash << 5) - hash) + playerId.charCodeAt(i);
            hash |= 0; // Convert to 32-bit int
        }
        return Math.abs(hash) % this.SPAWN_POSITIONS.length;
    }

    createPlayerTank(playerId: string, tankType?: string, playerIndex?: number): Tank {
        const idx = playerIndex !== undefined
            ? playerIndex
            : this.hashPlayerIdToIndex(playerId);
        const spawn = this.SPAWN_POSITIONS[idx % this.SPAWN_POSITIONS.length];

        return {
            id: playerId,
            x: spawn.x,
            y: spawn.y,
            rotation: spawn.rotation,
            color: '#66bb6a',
            width: 36,
            height: 36,
            speed: this.config.tankSpeed,
            tankType: tankType || 'e100'
        };
    }

    createBullet(tank: Tank): Bullet {
        return {
            id: `bullet_${Date.now()}`,
            x: tank.x,
            y: tank.y,
            rotation: tank.rotation,
            speed: this.config.bulletSpeed,
            ownerId: tank.id
        };
    }

    checkCollisionWithObstacles(x: number, y: number, width: number, height: number, obstacles: Obstacle[]): boolean {
        const halfW = width / 2;
        const halfH = height / 2;

        for (const obs of obstacles) {
            if (
                x - halfW < obs.x + obs.width &&
                x + halfW > obs.x &&
                y - halfH < obs.y + obs.height &&
                y + halfH > obs.y
            ) {
                return true;
            }
        }
        return false;
    }

    clampToCanvas(x: number, y: number, width: number, height: number): { x: number; y: number } {
        return {
            x: Math.max(width / 2, Math.min(this.config.canvasWidth - width / 2, x)),
            y: Math.max(height / 2, Math.min(this.config.canvasHeight - height / 2, y))
        };
    }

    getRotationForDirection(direction: 'up' | 'down' | 'left' | 'right'): number {
        const rotations = { up: 0, down: 180, left: 270, right: 90 };
        return rotations[direction];
    }

    updateBullets(bullets: Bullet[], obstacles: Obstacle[]): {
        updatedBullets: Bullet[];
        destroyedObstacles: string[];
        updatedObstacles: Obstacle[];
    } {
        const destroyedObstacles: string[] = [];
        const updatedObstacles = [...obstacles];
        const updatedBullets: Bullet[] = [];

        for (const bullet of bullets) {
            const rad = (bullet.rotation * Math.PI) / 180;
            const newX = bullet.x + Math.sin(rad) * bullet.speed;
            const newY = bullet.y - Math.cos(rad) * bullet.speed;

            if (newX < 0 || newX > this.config.canvasWidth || newY < 0 || newY > this.config.canvasHeight) {
                continue;
            }

            let hitObstacle = false;
            for (let j = updatedObstacles.length - 1; j >= 0; j--) {
                const obs = updatedObstacles[j];
                if (
                    newX > obs.x &&
                    newX < obs.x + obs.width &&
                    newY > obs.y &&
                    newY < obs.y + obs.height
                ) {
                    hitObstacle = true;
                    if (obs.destructible) {
                        obs.health--;
                        if (obs.health <= 0) {
                            destroyedObstacles.push(obs.id);
                            updatedObstacles.splice(j, 1);
                        }
                    }
                    break;
                }
            }

            if (!hitObstacle) {
                updatedBullets.push({ ...bullet, x: newX, y: newY });
            }
        }

        return { updatedBullets, destroyedObstacles, updatedObstacles };
    }

    /**
     * Check if a position collides with any other tank (AABB collision)
     */
    checkCollisionWithTanks(x: number, y: number, width: number, height: number, selfId: string, otherTanks: Tank[]): boolean {
        const halfW = width / 2;
        const halfH = height / 2;

        for (const other of otherTanks) {
            if (other.id === selfId) continue;
            const oHalfW = other.width / 2;
            const oHalfH = other.height / 2;

            if (
                x - halfW < other.x + oHalfW &&
                x + halfW > other.x - oHalfW &&
                y - halfH < other.y + oHalfH &&
                y + halfH > other.y - oHalfH
            ) {
                return true;
            }
        }
        return false;
    }

    calculateTankMovement(
        tank: Tank,
        keys: Set<string>,
        obstacles: Obstacle[],
        otherTanks: Tank[] = []
    ): { newX: number; newY: number; newRotation: number; moved: boolean } {
        let newX = tank.x;
        let newY = tank.y;
        let newRotation = tank.rotation;
        let moved = false;

        if (keys.has('w') || keys.has('arrowup')) {
            newY -= tank.speed;
            newRotation = 0;
            moved = true;
        }
        if (keys.has('s') || keys.has('arrowdown')) {
            newY += tank.speed;
            newRotation = 180;
            moved = true;
        }
        if (keys.has('a') || keys.has('arrowleft')) {
            newX -= tank.speed;
            newRotation = 270;
            moved = true;
        }
        if (keys.has('d') || keys.has('arrowright')) {
            newX += tank.speed;
            newRotation = 90;
            moved = true;
        }

        // Check collision with obstacles
        if (this.checkCollisionWithObstacles(newX, newY, tank.width, tank.height, obstacles)) {
            newX = tank.x;
            newY = tank.y;
        }

        // Check collision with other tanks
        if (this.checkCollisionWithTanks(newX, newY, tank.width, tank.height, tank.id, otherTanks)) {
            newX = tank.x;
            newY = tank.y;
        }

        const clamped = this.clampToCanvas(newX, newY, tank.width, tank.height);

        return {
            newX: clamped.x,
            newY: clamped.y,
            newRotation,
            moved: moved && (clamped.x !== tank.x || clamped.y !== tank.y || newRotation !== tank.rotation)
        };
    }

    getRandomColor(): string {
        const colors = ['#F44336', '#2196F3', '#FF9800', '#9C27B0', '#00BCD4'];
        return colors[Math.floor(Math.random() * colors.length)];
    }
}
