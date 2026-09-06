import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameApiService } from './services/game-api.service';
import { GameMode, GameState } from './models/game';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private readonly api = inject(GameApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  game?: GameState;
  selectedMode: GameMode = 'TwoPlayer';
  error = '';
  busy = false;

ngOnInit(): void {
  console.log('APP INITIALIZED');
  this.newGame();
}

newGame(mode: GameMode = this.selectedMode): void {
  console.log('NEW GAME CALLED', mode);

  this.selectedMode = mode;

  this.api.createGame(mode).subscribe({
    next: game => {
      console.log('GAME RECEIVED', game);
      this.game = game;
      this.busy = false;
    },
    error: err => {
      console.error('GAME API ERROR', err);
      this.error = err?.error?.message ?? 'Request failed.';
      this.busy = false;
    }
  });
}

  play(row: number, column: number): void {
    if (!this.game || this.busy || this.game.status !== 'InProgress' || this.game.board[row][column]) return;
    if (this.game.mode === 'Computer' && this.game.currentPlayer === 'O') return;
    this.run(() => this.api.move(this.game!.id, this.game!.currentPlayer, row, column));
  }

  undo(): void {
    if (this.game) this.run(() => this.api.undo(this.game!.id));
  }

  resetGame(): void {
    if (this.game) this.run(() => this.api.resetGame(this.game!.id));
  }

  resetScoreboard(): void {
    this.error = '';
    this.api.resetScoreboard().subscribe({
      next: score => {
        if (this.game) {
          this.game = { ...this.game, scoreboard: score };
          this.cdr.detectChanges();
        }
      },
      
      error: err => this.error = err?.error?.message ?? 'Could not reset scoreboard.'
    });
  }

  isWinning(row: number, column: number): boolean {
    return this.game?.winningCells.some(c => c[0] === row && c[1] === column) ?? false;
  }

  statusText(): string {
    if (!this.game) return 'Loading…';
    if (this.game.status === 'Won') return `${this.game.winner} wins!`;
    if (this.game.status === 'Draw') return 'Draw game';
    return `${this.game.currentPlayer}'s turn`;
  }

private run(action: () => import('rxjs').Observable<GameState>): void {
  this.busy = true;
  this.error = '';

  action().subscribe({
    next: game => {
      this.game = game;
      this.busy = false;
      this.cdr.detectChanges();
    },
    error: err => {
      this.error = err?.error?.message ?? 'Request failed.';
      this.busy = false;
      this.cdr.detectChanges();
    }
  });
}
}
