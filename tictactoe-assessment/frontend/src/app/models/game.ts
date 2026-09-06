export type GameMode = 'TwoPlayer' | 'Computer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface MoveRecord {
  moveNumber: number;
  player: 'X' | 'O';
  row: number;
  column: number;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameState {
  id: string;
  board: (string | null)[][];
  currentPlayer: 'X' | 'O';
  mode: GameMode;
  status: GameStatus;
  winner: string | null;
  winningCells: number[][];
  moveHistory: MoveRecord[];
  scoreboard: Scoreboard;
}
