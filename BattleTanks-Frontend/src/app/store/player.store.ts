import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { computed } from '@angular/core';
import { PlayerPosition } from '../services/game.service';

export type PlayerStatus = 'alive' | 'wounded' | 'dead';

export interface PlayerState {
  id: string;
  name: string;
  isReady: boolean;
  x: number;
  y: number;
  rotation: number;
  health: number;
  maxHealth: number;
  color: string;
  score: number;
  ammo: number;
  maxAmmo: number;
  status: PlayerStatus;
  tankType: string;
}

interface GameState {
  players: PlayerState[];
  currentPlayerId: string;
  totalScore: number;
}

const initialState: GameState = {
  players: [],
  currentPlayerId: '',
  totalScore: 0,
};

// Crear store para jugadores
export const PlayerStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    currentPlayer: computed(() => {
      return store.players().find(p => p.id === store.currentPlayerId()) || null;
    }),
    currentPlayerStatus: computed(() => {
      const player = store.players().find(p => p.id === store.currentPlayerId());
      return player?.status || 'alive';
    }),
    currentPlayerLives: computed(() => {
      const player = store.players().find(p => p.id === store.currentPlayerId());
      return player?.health || 0;
    }),
    currentPlayerScore: computed(() => {
      const player = store.players().find(p => p.id === store.currentPlayerId());
      return player?.score || 0;
    }),
    currentPlayerAmmo: computed(() => {
      const player = store.players().find(p => p.id === store.currentPlayerId());
      return player?.ammo || 0;
    }),
    alivePlayers: computed(() =>
      store.players().filter(p => p.status !== 'dead')
    ),
    aliveCount: computed(() =>
      store.players().filter(p => p.status !== 'dead').length
    ),
    livesArray: computed(() => {
      const player = store.players().find(p => p.id === store.currentPlayerId());
      const health = player?.health || 0;
      const maxHealth = player?.maxHealth || 3;
      return Array.from({ length: maxHealth }, (_, i) => i < health);
    }),
  })),
  withMethods((store) => ({

    setCurrentPlayerId(playerId: string): void {
      patchState(store, { currentPlayerId: playerId });
    },

    setPlayers(players: PlayerState[]): void {
      console.log('[Store] Inicializando lista de jugadores:', players.length);
      patchState(store, { players });
    },

    addPlayer(player: Partial<PlayerState> & { id: string; name: string }): void {
      const exists = store.players().some(p => p.id === player.id);
      if (!exists) {
        console.log('[Store] Agregando jugador:', player.name);
        const newPlayer: PlayerState = {
          id: player.id,
          name: player.name,
          isReady: player.isReady || false,
          x: player.x || 0,
          y: player.y || 0,
          rotation: player.rotation || 0,
          health: player.health || 3,
          maxHealth: player.maxHealth || 3,
          color: player.color || '#4CAF50',
          score: player.score || 0,
          ammo: player.ammo || 10,
          maxAmmo: player.maxAmmo || 10,
          status: player.status || 'alive',
          tankType: player.tankType || 'e100',
        };
        patchState(store, (state) => ({
          players: [...state.players, newPlayer],
        }));
      }
    },

    removePlayer(id: string): void {
      console.log('[Store] Eliminando jugador ID:', id);
      patchState(store, (state) => ({
        players: state.players.filter((p) => p.id !== id),
      }));
    },

    updatePlayerPosition(position: PlayerPosition): void {
      patchState(store, (state) => ({
        players: state.players.map((p) =>
          p.id === position.playerId
            ? { ...p, x: position.x, y: position.y, rotation: position.rotation }
            : p
        ),
      }));
    },

    updatePlayerReady(id: string, isReady: boolean): void {
      console.log(`[Store] Estado Ready actualizado para ${id}:`, isReady);
      patchState(store, (state) => ({
        players: state.players.map(p =>
          p.id === id ? { ...p, isReady } : p
        )
      }));
    },

    incrementScore(playerId: string, points: number): void {
      console.log(`[Store] +${points} puntos para ${playerId}`);
      patchState(store, (state) => ({
        players: state.players.map(p =>
          p.id === playerId ? { ...p, score: p.score + points } : p
        ),
        totalScore: state.totalScore + points,
      }));
    },

    decrementAmmo(playerId: string): boolean {
      const player = store.players().find(p => p.id === playerId);
      if (!player || player.ammo <= 0) {
        console.log('[Store] Sin municiones');
        return false;
      }
      patchState(store, (state) => ({
        players: state.players.map(p =>
          p.id === playerId ? { ...p, ammo: p.ammo - 1 } : p
        ),
      }));
      return true;
    },

    reloadAmmo(playerId: string): void {
      patchState(store, (state) => ({
        players: state.players.map(p =>
          p.id === playerId ? { ...p, ammo: p.maxAmmo } : p
        ),
      }));
    },

    takeDamage(playerId: string, damage: number = 1): void {
      patchState(store, (state) => ({
        players: state.players.map(p => {
          if (p.id !== playerId) return p;

          const newHealth = Math.max(0, p.health - damage);
          let newStatus: PlayerStatus = 'alive';

          if (newHealth === 0) {
            newStatus = 'dead';
          } else if (newHealth <= p.maxHealth / 2) {
            newStatus = 'wounded';
          }

          console.log(`[Store] Jugador ${p.name}: salud=${newHealth}, estado=${newStatus}`);
          return { ...p, health: newHealth, status: newStatus };
        }),
      }));
    },

    heal(playerId: string, amount: number = 1): void {
      patchState(store, (state) => ({
        players: state.players.map(p => {
          if (p.id !== playerId) return p;

          const newHealth = Math.min(p.maxHealth, p.health + amount);
          const newStatus: PlayerStatus = newHealth > p.maxHealth / 2 ? 'alive' : 'wounded';

          return { ...p, health: newHealth, status: newStatus };
        }),
      }));
    },

    resetPlayer(playerId: string): void {
      patchState(store, (state) => ({
        players: state.players.map(p =>
          p.id === playerId
            ? { ...p, health: p.maxHealth, ammo: p.maxAmmo, score: 0, status: 'alive' as PlayerStatus }
            : p
        ),
      }));
    },

    updateTankType(playerId: string, tankType: string): void {
      console.log(`[Store] Tanque actualizado para ${playerId}: ${tankType}`);
      patchState(store, (state) => ({
        players: state.players.map(p =>
          p.id === playerId ? { ...p, tankType } : p
        ),
      }));
    },
  }))
);
