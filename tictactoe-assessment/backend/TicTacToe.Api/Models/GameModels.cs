namespace TicTacToe.Api.Models;

public enum GameMode { TwoPlayer, Computer }
public enum GameStatus { InProgress, Won, Draw }

public sealed record MoveRecord(int MoveNumber, string Player, int Row, int Column);
public sealed record Scoreboard(int XWins, int OWins, int Draws);

public sealed class GameState
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string?[][] Board { get; set; } = CreateEmptyBoard();
    public string CurrentPlayer { get; set; } = "X";
    public GameMode Mode { get; init; }
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public string? Winner { get; set; }
    public List<int[]> WinningCells { get; set; } = [];
    public List<MoveRecord> MoveHistory { get; set; } = [];
    public bool ScoreRecorded { get; set; }

    public static string?[][] CreateEmptyBoard() =>
    [
        [null, null, null],
        [null, null, null],
        [null, null, null]
    ];
}

public sealed record CreateGameRequest(GameMode Mode);
public sealed record MoveRequest(string Player, int Row, int Column);
public sealed record GameResponse(
    Guid Id,
    string?[][] Board,
    string CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    string? Winner,
    IReadOnlyList<int[]> WinningCells,
    IReadOnlyList<MoveRecord> MoveHistory,
    Scoreboard Scoreboard);
