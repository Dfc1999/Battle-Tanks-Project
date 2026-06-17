import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WaitingRoomComponent } from './waiting-room.component';
import { GameService } from '../../services/game.service';
import { PlayerStore } from '../../store/player.store';
import { of } from 'rxjs';

const mockGameService = {
  connect: () => Promise.resolve(),
  disconnect: () => Promise.resolve(),

  onCurrentPlayers: () => of([]),
  onPlayerJoined: () => of({}),
  onPlayerLeft: () => of(''),
  onChatMessage: () => of({}),

  sendChatMessage: () => {},

  isConnected: () => true
};

describe('WaitingRoomComponent', () => {
  let component: WaitingRoomComponent;
  let fixture: ComponentFixture<WaitingRoomComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WaitingRoomComponent],
      providers: [
        PlayerStore,
        { provide: GameService, useValue: mockGameService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WaitingRoomComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
