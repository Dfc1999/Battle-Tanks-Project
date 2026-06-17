export type TileType = 0 | 1 | 2;

export interface MapData {
    name: string;
    description: string;
    tileSize: number;
    legend: Record<string, string>;
    tiles: TileType[][];
}

export interface Explosion {
    id: string;
    x: number;
    y: number;
    frame: number;
    maxFrames: number;
}

export interface TankType {
    id: string;
    name: string;
    spriteUrl: string;
    description: string;
}

export const TANK_TYPES: TankType[] = [
    {
        id: 'e100',
        name: 'E-100',
        spriteUrl: 'assets/tanks/E-100_preview.png',
        description: 'Tanque pesado con camuflaje artico'
    },
    {
        id: 'kv2',
        name: 'KV-2',
        spriteUrl: 'assets/tanks/KV-2_preview.png',
        description: 'Tanque sovietico de asalto'
    },
    {
        id: 'pzkpfw',
        name: 'Pz.Kpfw.IV',
        spriteUrl: 'assets/tanks/Pz.Kpfw.IV_preview.png',
        description: 'Tanque medio aleman versatil'
    },
    {
        id: 'tiger2',
        name: 'Tiger II',
        spriteUrl: 'assets/tanks/Tiger-II_preview.png',
        description: 'Blindaje pesado del desierto'
    }
];

export interface Tank {
    id: string;
    x: number;
    y: number;
    rotation: number;
    color: string;
    width: number;
    height: number;
    speed: number;
    tankType?: string;
}

export interface Bullet {
    id: string;
    x: number;
    y: number;
    rotation: number;
    speed: number;
    ownerId: string;
    isRemote?: boolean;
}

export interface Obstacle {
    id: string;
    x: number;
    y: number;
    width: number;
    height: number;
    destructible: boolean;
    health: number;
    color: string;
}

export interface GameConfig {
    canvasWidth: number;
    canvasHeight: number;
    tileSize: number;
    tankSpeed: number;
    bulletSpeed: number;
}

export const DEFAULT_GAME_CONFIG: GameConfig = {
    canvasWidth: 800,
    canvasHeight: 600,
    tileSize: 40,
    tankSpeed: 1.8,
    bulletSpeed: 5
};

// Interfaz para Power-Ups
export type PowerUpType = 'health' | 'ammo' | 'speed';

export interface PowerUp {
    id: string;
    x: number;
    y: number;
    type: PowerUpType;
    sentTimestamp?: number;
}
