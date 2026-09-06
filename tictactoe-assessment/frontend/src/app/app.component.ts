import { Component, OnInit, inject, signal } from '@angular/core';
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

  game = signal<GameState | undefined>(undefined);

  selectedMode: GameMode = 'TwoPlayer';
  error = '';
  busy = false;

  ngOnInit(): void {
    this.newGame();
  }

  newGame(mode: GameMode = this.selectedMode): void {
    this.selectedMode = mode;
    this.run(() => this.api.createGame(mode));
  }

  play(row: number, column: number): void {
    const g = this.game();

    if (
      !g ||
      this.busy ||
      g.status !== 'InProgress' ||
      g.board[row][column]
    ) {
      return;
    }

    if (g.mode === 'Computer' && g.currentPlayer === 'O') {
      return;
    }

    this.run(() =>
      this.api.move(
        g.id,
        g.currentPlayer,
        row,
        column
      )
    );
  }

  undo(): void {
    const g = this.game();

    if (g) {
      this.run(() => this.api.undo(g.id));
    }
  }

  resetGame(): void {
    const g = this.game();

    if (g) {
      this.run(() => this.api.resetGame(g.id));
    }
  }

  resetScoreboard(): void {
    this.error = '';

    this.api.resetScoreboard().subscribe({
      next: score => {
        const g = this.game();

        if (g) {
          this.game.set({
            ...g,
            scoreboard: score
          });
        }
      },

      error: err => {
        this.error =
          err?.error?.message ??
          'Could not reset scoreboard.';
      }
    });
  }

  isWinning(row: number, column: number): boolean {
    return (
      this.game()?.winningCells.some(
        c => c[0] === row && c[1] === column
      ) ?? false
    );
  }

  statusText(): string {
    const g = this.game();

    if (!g) return 'Loading…';

    if (g.status === 'Won') {
      return `${g.winner} wins!`;
    }

    if (g.status === 'Draw') {
      return 'Draw game';
    }

    return `${g.currentPlayer}'s turn`;
  }

  private run(
    action: () => import('rxjs').Observable<GameState>
  ): void {

    this.busy = true;
    this.error = '';

    action().subscribe({
      next: game => {
        console.log('GAME RECEIVED', game);

        this.game.set(game);
        this.busy = false;
      },

      error: err => {
        console.error('GAME API ERROR', err);

        this.error =
          err?.error?.message ??
          'Request failed.';

        this.busy = false;
      }
    });
  }
}
