import { TestBed } from '@angular/core/testing';
import { PlayerStore } from './player.store';

// Pruebas unitarias
describe('PlayerStore', () => {
  let store: InstanceType<typeof PlayerStore>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PlayerStore],
    });
    store = TestBed.inject(PlayerStore);
  });

  it('should initialize with an empty player list', () => {
    expect(store.players().length).toBe(0);
  });

  it('should add a player to the store (addPlayer)', () => {
    const newPlayer = {
      id: 'p1',
      name: 'Unit Tester',
      isReady: false,
      x: 0,
      y: 0,
      rotation: 0,
      health: 100,
      color: '#000000'
    };

    store.addPlayer(newPlayer);

    const players = store.players();
    expect(players.length).toBe(1);
    expect(players[0].name).toBe('Unit Tester');
    expect(players[0].id).toBe('p1');
  });

  it('should not add a duplicate player', () => {
    const player = {
      id: 'p1',
      name: 'Unit Tester',
      isReady: false,
      x: 0, y: 0, rotation: 0, health: 100, color: '#000000'
    };

    store.addPlayer(player);
    store.addPlayer(player);

    expect(store.players().length).toBe(1);
  });

  it('should remove a player by ID (removePlayer)', () => {
    store.addPlayer({
      id: 'p1', name: 'To Be Removed', isReady: false,
      x: 0, y: 0, rotation: 0, health: 100, color: 'red'
    });

    expect(store.players().length).toBe(1);

    store.removePlayer('p1');

    expect(store.players().length).toBe(0);
  });

  it('should update player position and rotation (updatePlayerPosition)', () => {
    store.addPlayer({
      id: 'p1', name: 'Mover', isReady: false,
      x: 0, y: 0, rotation: 0, health: 100, color: 'blue'
    });

    store.updatePlayerPosition({
      playerId: 'p1',
      x: 100,
      y: 100,
      rotation: 90
    });

    const player = store.players().find(p => p.id === 'p1');
    expect(player).toBeTruthy();
    expect(player?.x).toBe(100);
    expect(player?.y).toBe(100);
    expect(player?.rotation).toBe(90);
  });

  it('should toggle player ready status (updatePlayerReady)', () => {
    store.addPlayer({
      id: 'p1', name: 'ReadyTester', isReady: false,
      x: 0, y: 0, rotation: 0, health: 100, color: 'green'
    });

    store.updatePlayerReady('p1', true);

    expect(store.players()[0].isReady).toBe(true);

    store.updatePlayerReady('p1', false);

    expect(store.players()[0].isReady).toBe(false);
  });
});
