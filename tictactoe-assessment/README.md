# Tic Tac Toe — Angular + .NET Assessment

A browser-based Tic Tac Toe application built for a technical panel exercise. The Angular frontend renders the UI and sends actions through REST APIs; the .NET backend is the source of truth for game rules, session state, history, computer moves, and the session-level scoreboard.

## Tech stack

- Angular 22 + TypeScript 6
- .NET 10 Web API / C#
- REST API
- In-memory state
- xUnit backend tests

## Features

- 3×3 Tic Tac Toe board
- Two Player mode
- Human vs Computer mode (human = X, computer = O)
- Current turn display
- Backend validation of invalid/out-of-turn/occupied/out-of-range moves
- Row, column, and diagonal win detection
- Winning-cell highlighting
- Draw detection
- Move history
- Undo
  - Two Player: removes one move
  - Computer: removes the human/computer move pair
- Reset Game while preserving scoreboard
- Session scoreboard for X wins, O wins, and draws
- Reset Scoreboard

## Design decisions

### Backend owns state

The frontend does not calculate wins, decide turns, or generate computer moves. Every action returns the latest authoritative `GameResponse` from the API.

### In-memory storage

`ConcurrentDictionary<Guid, GameState>` stores games and a singleton `GameService` stores the scoreboard. This is intentionally simple for a local assessment. Restarting the API clears all state.

### Undo after completion

This implementation uses **Clarification Option A: Disable Undo After Completion**. Once a game is won or drawn, the result and scoreboard are final for that game. This avoids compensating scoreboard transactions and keeps the rule easy to explain and test.

### Computer strategy

The computer follows the required deterministic priority:

1. Win immediately if possible
2. Block X's immediate win
3. Take center
4. Take the first available corner
5. Take the first available cell

Deterministic selection makes automated tests repeatable.

## Prerequisites

- .NET 10 SDK
- Node.js version supported by Angular 22 (for Angular 22.0.x, Node `^22.22.3`, `^24.15.0`, or `^26.0.0`)
- npm

## Run locally

### 1. Backend

```bash
cd backend/TicTacToe.Api
dotnet restore
dotnet run
```

The API runs at:

```text
http://localhost:5000
```

### 2. Frontend

Open a second terminal:

```bash
cd frontend
npm install
npm start
```

Open:

```text
http://localhost:4200
```

The API CORS policy permits the Angular development origin above.

## API contract

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/games` | Create game (`TwoPlayer` or `Computer`) |
| GET | `/api/games/{id}` | Get current state |
| POST | `/api/games/{id}/moves` | Submit a move |
| POST | `/api/games/{id}/undo` | Undo according to game mode |
| POST | `/api/games/{id}/reset` | Reset board/history while keeping scoreboard |
| GET | `/api/scoreboard` | Get scoreboard |
| POST | `/api/scoreboard/reset` | Reset scoreboard |

### Create game

```http
POST /api/games
Content-Type: application/json

{
  "mode": "Computer"
}
```

### Submit move

```http
POST /api/games/{id}/moves
Content-Type: application/json

{
  "player": "X",
  "row": 0,
  "column": 0
}
```

Rows and columns are zero-based in the API. The UI presents them as Row 1–3 / Column 1–3.

### Game response shape

```json
{
  "id": "guid",
  "board": [["X", null, null], [null, "O", null], [null, null, null]],
  "currentPlayer": "X",
  "mode": "Computer",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "moveHistory": [
    { "moveNumber": 1, "player": "X", "row": 0, "column": 0 },
    { "moveNumber": 2, "player": "O", "row": 1, "column": 1 }
  ],
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

Invalid moves return HTTP `400` with a message and the unchanged current state. Missing games return `404`.

## Tests

Run backend tests:

```bash
cd backend/TicTacToe.Tests
dotnet test
```

The test suite covers the requested core behavior, including:

- valid move / turn switching
- invalid occupied-cell move
- row win
- column win
- diagonal win
- draw
- reset game and scoreboard preservation
- two-player undo
- computer-mode pair undo
- scoreboard updates only once
- move after completion rejection
- computer center choice
- computer blocking move

## AI-assisted development notes

### How the requirement was converted into a specification

The requirements were separated into four responsibilities:

1. **Domain state:** board, current player, status, winner, history
2. **Domain transitions:** move, evaluate, undo, reset, computer move
3. **Session concerns:** game lookup and scoreboard
4. **Presentation/API:** REST controllers and Angular rendering

### Example prompts used

- “Convert these Tic Tac Toe requirements into backend domain invariants and REST operations.”
- “Implement deterministic computer-move logic using win, block, center, corner, fallback priority.”
- “Generate backend unit-test scenarios for valid/invalid moves, wins, draws, undo, scoreboard, and computer mode.”
- “Create a minimal Angular standalone UI that renders only server-returned game state.”
- “Review the solution for race conditions, scoreboard double-counting, and undo consistency.”

### What AI generated

AI assisted with the initial project structure, repetitive models/controllers, test case scaffolding, and CSS layout.

### What should be manually reviewed

The most important manually reviewed areas are:

- server-side move validation
- state transitions and current-player changes
- scoreboard's update-once rule
- computer strategy priority
- undo semantics by game mode
- enum serialization between .NET and Angular
- CORS and local ports

### Assumptions

- A “session-level scoreboard” means the lifetime of the running backend process.
- New games share one scoreboard.
- Resetting a completed game starts a new round under the same game ID.
- Undo is disabled after win/draw (Option A).
- Computer moves are synchronous with the human move API call; the returned response already contains both moves.

## Known limitations

- In-memory state is lost when the API restarts.
- No authentication or multi-user ownership model.
- No persistence across multiple backend instances.
- Frontend automated tests are not included; game rules are concentrated in backend unit tests.
- No difficulty levels beyond the specified basic strategy.

## Future improvements

- SQLite/EF Core persistence
- integration/API tests with `WebApplicationFactory`
- Angular component/API tests
- cancellation/loading UX improvements
- configurable computer difficulty or minimax
- Docker Compose for one-command startup
- OpenAPI/Swagger documentation
- CI workflow for build and tests

## Panel walkthrough

A concise review flow:

1. Start both apps and create a Two Player game.
2. Demonstrate valid/invalid moves and turn switching.
3. Complete a win and show winning-cell highlighting + scoreboard.
4. Reset the game and show the scoreboard remains.
5. Demonstrate one-move Undo in Two Player mode.
6. Switch to Computer mode and show the automatic O move.
7. Demonstrate pair Undo.
8. Open `GameService.cs` to discuss rule ownership and computer priority.
9. Open `GameServiceTests.cs` to discuss test coverage and AI-assisted review.
