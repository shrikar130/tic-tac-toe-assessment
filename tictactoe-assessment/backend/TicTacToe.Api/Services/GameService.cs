using System.Collections.Concurrent;
using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public sealed class GameService
{
    private readonly ConcurrentDictionary<Guid, GameState> _games = new();
    private readonly object _scoreLock = new();
    private int _xWins;
    private int _oWins;
    private int _draws;

    public GameState CreateGame(GameMode mode)
    {
        var game = new GameState { Mode = mode };
        _games[game.Id] = game;
        return game;
    }

    public GameState? GetGame(Guid id) => _games.TryGetValue(id, out var game) ? game : null;

    public Scoreboard GetScoreboard()
    {
        lock (_scoreLock) return new Scoreboard(_xWins, _oWins, _draws);
    }

    public void ResetScoreboard()
    {
        lock (_scoreLock) (_xWins, _oWins, _draws) = (0, 0, 0);
    }

    public (GameState? Game, string? Error) MakeMove(Guid id, MoveRequest request)
    {
        if (!_games.TryGetValue(id, out var game)) return (null, "Game not found.");

        lock (game)
        {
            var validationError = ValidateMove(game, request);
            if (validationError is not null) return (game, validationError);

            ApplyMove(game, request.Player, request.Row, request.Column);
            EvaluateGame(game);

            if (game.Status == GameStatus.InProgress)
            {
                game.CurrentPlayer = game.CurrentPlayer == "X" ? "O" : "X";

                if (game.Mode == GameMode.Computer && game.CurrentPlayer == "O")
                {
                    var move = ChooseComputerMove(game);
                    if (move is not null)
                    {
                        ApplyMove(game, "O", move.Value.Row, move.Value.Column);
                        EvaluateGame(game);
                        if (game.Status == GameStatus.InProgress) game.CurrentPlayer = "X";
                    }
                }
            }

            RecordScoreIfNeeded(game);
            return (game, null);
        }
    }

    public (GameState? Game, string? Error) Undo(Guid id)
    {
        if (!_games.TryGetValue(id, out var game)) return (null, "Game not found.");

        lock (game)
        {
            if (game.Status != GameStatus.InProgress)
                return (game, "Undo is disabled after game completion.");
            if (game.MoveHistory.Count == 0)
                return (game, "There are no moves to undo.");

            var count = game.Mode == GameMode.Computer ? Math.Min(2, game.MoveHistory.Count) : 1;
            for (var i = 0; i < count; i++)
            {
                var last = game.MoveHistory[^1];
                game.Board[last.Row][last.Column] = null;
                game.MoveHistory.RemoveAt(game.MoveHistory.Count - 1);
            }

            Recalculate(game);
            game.CurrentPlayer = game.Mode == GameMode.Computer
                ? "X"
                : (game.MoveHistory.Count % 2 == 0 ? "X" : "O");
            return (game, null);
        }
    }

    public GameState? Reset(Guid id)
    {
        if (!_games.TryGetValue(id, out var game)) return null;
        lock (game)
        {
            game.Board = GameState.CreateEmptyBoard();
            game.CurrentPlayer = "X";
            game.Status = GameStatus.InProgress;
            game.Winner = null;
            game.WinningCells = [];
            game.MoveHistory.Clear();
            game.ScoreRecorded = false;
            return game;
        }
    }

    public GameResponse ToResponse(GameState game) => new(
        game.Id, game.Board, game.CurrentPlayer, game.Mode, game.Status, game.Winner,
        game.WinningCells, game.MoveHistory, GetScoreboard());

    private static string? ValidateMove(GameState game, MoveRequest request)
    {
        if (game.Status != GameStatus.InProgress) return "The game is already completed.";
        if (request.Row is < 0 or > 2 || request.Column is < 0 or > 2) return "Move is outside the board.";
        if (request.Player != game.CurrentPlayer) return "It is not this player's turn.";
        if (game.Mode == GameMode.Computer && request.Player != "X") return "Only player X is human in computer mode.";
        if (game.Board[request.Row][request.Column] is not null) return "Cell is already occupied.";
        return null;
    }

    private static void ApplyMove(GameState game, string player, int row, int column)
    {
        game.Board[row][column] = player;
        game.MoveHistory.Add(new MoveRecord(game.MoveHistory.Count + 1, player, row, column));
    }

    private static void EvaluateGame(GameState game)
    {
        var lines = new[]
        {
            new[]{(0,0),(0,1),(0,2)}, new[]{(1,0),(1,1),(1,2)}, new[]{(2,0),(2,1),(2,2)},
            new[]{(0,0),(1,0),(2,0)}, new[]{(0,1),(1,1),(2,1)}, new[]{(0,2),(1,2),(2,2)},
            new[]{(0,0),(1,1),(2,2)}, new[]{(0,2),(1,1),(2,0)}
        };

        foreach (var line in lines)
        {
            var value = game.Board[line[0].Item1][line[0].Item2];
            if (value is not null && line.All(c => game.Board[c.Item1][c.Item2] == value))
            {
                game.Status = GameStatus.Won;
                game.Winner = value;
                game.WinningCells = line.Select(c => new[] { c.Item1, c.Item2 }).ToList();
                return;
            }
        }

        if (game.Board.SelectMany(r => r).All(c => c is not null))
        {
            game.Status = GameStatus.Draw;
            game.Winner = null;
            game.WinningCells = [];
            return;
        }

        game.Status = GameStatus.InProgress;
        game.Winner = null;
        game.WinningCells = [];
    }

    private static void Recalculate(GameState game)
    {
        game.ScoreRecorded = false;
        EvaluateGame(game);
    }

    private void RecordScoreIfNeeded(GameState game)
    {
        if (game.Status == GameStatus.InProgress || game.ScoreRecorded) return;
        lock (_scoreLock)
        {
            if (game.Status == GameStatus.Draw) _draws++;
            else if (game.Winner == "X") _xWins++;
            else if (game.Winner == "O") _oWins++;
            game.ScoreRecorded = true;
        }
    }

    public static (int Row, int Column)? ChooseComputerMove(GameState game)
    {
        var available = Enumerable.Range(0, 3)
            .SelectMany(r => Enumerable.Range(0, 3).Select(c => (Row: r, Column: c)))
            .Where(p => game.Board[p.Row][p.Column] is null)
            .ToList();

        foreach (var p in available)
            if (WouldWin(game, "O", p.Row, p.Column)) return p;
        foreach (var p in available)
            if (WouldWin(game, "X", p.Row, p.Column)) return p;
        if (game.Board[1][1] is null) return (1, 1);

        foreach (var p in new[] { (0,0), (0,2), (2,0), (2,2) })
            if (game.Board[p.Item1][p.Item2] is null) return (p.Item1, p.Item2);

        return available.Count > 0 ? available[0] : null;
    }

    private static bool WouldWin(GameState game, string player, int row, int col)
    {
        game.Board[row][col] = player;
        var won = HasWinner(game, player);
        game.Board[row][col] = null;
        return won;
    }

    private static bool HasWinner(GameState game, string player)
    {
        for (var i = 0; i < 3; i++)
        {
            if (Enumerable.Range(0, 3).All(c => game.Board[i][c] == player)) return true;
            if (Enumerable.Range(0, 3).All(r => game.Board[r][i] == player)) return true;
        }
        return Enumerable.Range(0, 3).All(i => game.Board[i][i] == player)
            || Enumerable.Range(0, 3).All(i => game.Board[i][2 - i] == player);
    }
}
