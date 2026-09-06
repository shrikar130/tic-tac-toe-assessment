using Xunit;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    private static GameService Service() => new();

    [Fact]
    public void ValidMove_SwitchesTurn()
    {
        var s = Service(); var g = s.CreateGame(GameMode.TwoPlayer);
        var (state, error) = s.MakeMove(g.Id, new("X",0,0));
        Assert.Null(error); Assert.Equal("X", state!.Board[0][0]); Assert.Equal("O", state.CurrentPlayer);
    }

    [Fact]
    public void InvalidMove_DoesNotSwitchTurn()
    {
        var s = Service(); var g = s.CreateGame(GameMode.TwoPlayer);
        s.MakeMove(g.Id, new("X",0,0));
        var (state, error) = s.MakeMove(g.Id, new("O",0,0));
        Assert.NotNull(error); Assert.Equal("O", state!.CurrentPlayer);
    }

    [Theory]
    [InlineData(0,0, 1,0, 0,1, 1,1, 0,2)]
    [InlineData(0,0, 0,1, 1,0, 1,1, 2,0)]
    [InlineData(0,0, 0,1, 1,1, 0,2, 2,2)]
    public void DetectsWins(int x1r,int x1c,int o1r,int o1c,int x2r,int x2c,int o2r,int o2c,int x3r,int x3c)
    {
        var s = Service(); var g = s.CreateGame(GameMode.TwoPlayer);
        s.MakeMove(g.Id,new("X",x1r,x1c)); s.MakeMove(g.Id,new("O",o1r,o1c));
        s.MakeMove(g.Id,new("X",x2r,x2c)); s.MakeMove(g.Id,new("O",o2r,o2c));
        var (state, _) = s.MakeMove(g.Id,new("X",x3r,x3c));
        Assert.Equal(GameStatus.Won,state!.Status); Assert.Equal("X",state.Winner);
    }

    [Fact]
    public void DetectsDraw()
    {
        var s = Service(); var g = s.CreateGame(GameMode.TwoPlayer);
        var moves = new[]{("X",0,0),("O",0,1),("X",0,2),("O",1,1),("X",1,0),("O",1,2),("X",2,1),("O",2,0),("X",2,2)};
        GameState? state = null;
        foreach (var m in moves) (state, _) = s.MakeMove(g.Id,new(m.Item1,m.Item2,m.Item3));
        Assert.Equal(GameStatus.Draw,state!.Status);
    }

    [Fact]
    public void Reset_ClearsBoardButKeepsScoreboard()
    {
        var s = Service(); var g = s.CreateGame(GameMode.TwoPlayer);
        s.MakeMove(g.Id,new("X",0,0)); s.MakeMove(g.Id,new("O",1,0)); s.MakeMove(g.Id,new("X",0,1)); s.MakeMove(g.Id,new("O",1,1)); s.MakeMove(g.Id,new("X",0,2));
        Assert.Equal(1,s.GetScoreboard().XWins);
        var reset = s.Reset(g.Id)!;
        Assert.All(reset.Board.SelectMany(r=>r), Assert.Null); Assert.Equal(1,s.GetScoreboard().XWins);
    }

    [Fact]
    public void Undo_TwoPlayer_RemovesOneMove()
    {
        var s=Service(); var g=s.CreateGame(GameMode.TwoPlayer);
        s.MakeMove(g.Id,new("X",0,0)); s.MakeMove(g.Id,new("O",1,1));
        var (state,_) = s.Undo(g.Id);
        Assert.Single(state!.MoveHistory); Assert.Null(state.Board[1][1]); Assert.Equal("O",state.CurrentPlayer);
    }

    [Fact]
    public void Undo_Computer_RemovesMovePair()
    {
        var s=Service(); var g=s.CreateGame(GameMode.Computer);
        var (state,_) = s.MakeMove(g.Id,new("X",0,0));
        Assert.Equal(2,state!.MoveHistory.Count);
        (state,_) = s.Undo(g.Id);
        Assert.Empty(state!.MoveHistory); Assert.Equal("X",state.CurrentPlayer);
    }

    [Fact]
    public void Scoreboard_UpdatesOnce_AndMoveAfterCompletionRejected()
    {
        var s=Service(); var g=s.CreateGame(GameMode.TwoPlayer);
        s.MakeMove(g.Id,new("X",0,0)); s.MakeMove(g.Id,new("O",1,0)); s.MakeMove(g.Id,new("X",0,1)); s.MakeMove(g.Id,new("O",1,1));
        s.MakeMove(g.Id,new("X",0,2));
        var (_,error)=s.MakeMove(g.Id,new("O",2,2));
        Assert.NotNull(error); Assert.Equal(1,s.GetScoreboard().XWins);
    }

    [Fact]
    public void Computer_TakesCenter_WhenNoImmediateThreat()
    {
        var s=Service(); var g=s.CreateGame(GameMode.Computer);
        var (state,_) = s.MakeMove(g.Id,new("X",0,0));
        Assert.Equal("O",state!.Board[1][1]);
    }

    [Fact]
    public void Computer_BlocksImmediateWin()
    {
        var s=Service(); var g=s.CreateGame(GameMode.Computer);
        // Set up directly so the deterministic priority can be isolated.
        g.Board[0][0]="X"; g.Board[0][1]="X"; g.Board[1][1]="O";
        var move=GameService.ChooseComputerMove(g);
        Assert.Equal((0,2),move);
    }
}
