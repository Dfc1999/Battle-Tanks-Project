import { computed } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { Obstacle } from '../models/game.models';

export type TileType = 0 | 1 | 2;

export interface MapData {
    name: string;
    description: string;
    tileSize: number;
    legend?: Record<string, string>;
    tiles: TileType[][];
}

export interface Explosion {
    id: string;
    x: number;
    y: number;
    frame: number;
    maxFrames: number;
}

interface MapState {
    mapData: MapData | null;
    obstacles: Obstacle[];
    explosions: Explosion[];
    isLoaded: boolean;
}

const initialState: MapState = {
    mapData: null,
    obstacles: [],
    explosions: [],
    isLoaded: false,
};

export const MapStore = signalStore(
    { providedIn: 'root' },
    withState(initialState),
    withComputed((store) => ({
        destructibleCount: computed(() =>
            store.obstacles().filter(o => o.destructible).length
        ),
        indestructibleCount: computed(() =>
            store.obstacles().filter(o => !o.destructible).length
        ),
        activeExplosions: computed(() =>
            store.explosions().filter(e => e.frame < e.maxFrames)
        ),
    })),
    withMethods((store) => ({

        loadMap(mapData: MapData): void {
            const obstacles = this.convertTilesToObstacles(mapData.tiles, mapData.tileSize);
            patchState(store, {
                mapData,
                obstacles,
                isLoaded: true,
            });
            console.log(`[MapStore] Mapa "${mapData.name}" cargado: ${obstacles.length} obstáculos`);
        },

        convertTilesToObstacles(tiles: TileType[][], tileSize: number): Obstacle[] {
            const obstacles: Obstacle[] = [];

            tiles.forEach((row, rowIndex) => {
                row.forEach((tile, colIndex) => {
                    if (tile === 1 || tile === 2) {
                        obstacles.push({
                            id: `tile_${rowIndex}_${colIndex}`,
                            x: colIndex * tileSize,
                            y: rowIndex * tileSize,
                            width: tileSize,
                            height: tileSize,
                            destructible: tile === 2,
                            health: tile === 2 ? 1 : -1,
                            color: tile === 2 ? '#8B4513' : '#708090',
                        });
                    }
                });
            });

            return obstacles;
        },

        destroyObstacle(obstacleId: string): void {
            const obstacle = store.obstacles().find(o => o.id === obstacleId);
            if (!obstacle || !obstacle.destructible) return;

            const explosion: Explosion = {
                id: `explosion_${Date.now()}`,
                x: obstacle.x + obstacle.width / 2,
                y: obstacle.y + obstacle.height / 2,
                frame: 0,
                maxFrames: 8,
            };

            patchState(store, (state) => ({
                obstacles: state.obstacles.filter(o => o.id !== obstacleId),
                explosions: [...state.explosions, explosion],
            }));

            console.log(`[MapStore] Obstáculo ${obstacleId} destruido`);
        },

        updateExplosions(): void {
            patchState(store, (state) => ({
                explosions: state.explosions
                    .map(e => ({ ...e, frame: e.frame + 1 }))
                    .filter(e => e.frame < e.maxFrames),
            }));
        },


        updateObstacle(obstacleId: string, changes: Partial<Obstacle>): void {
            patchState(store, (state) => ({
                obstacles: state.obstacles.map(o =>
                    o.id === obstacleId ? { ...o, ...changes } : o
                ),
            }));
        },

        resetMap(): void {
            const mapData = store.mapData();
            if (mapData) {
                const obstacles = this.convertTilesToObstacles(mapData.tiles, mapData.tileSize);
                patchState(store, {
                    obstacles,
                    explosions: [],
                });
                console.log('[MapStore] Mapa reseteado');
            }
        },

        clearExplosions(): void {
            patchState(store, { explosions: [] });
        },

        getObstacleAt(x: number, y: number): Obstacle | undefined {
            return store.obstacles().find(o =>
                x >= o.x && x < o.x + o.width &&
                y >= o.y && y < o.y + o.height
            );
        },
    }))
);
