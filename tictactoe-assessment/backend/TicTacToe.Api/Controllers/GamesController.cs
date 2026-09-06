using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(GameService service) : ControllerBase
{
    [HttpPost]
    public ActionResult<GameResponse> Create(CreateGameRequest request)
    {
        var game = service.CreateGame(request.Mode);
        return CreatedAtAction(nameof(Get), new { id = game.Id }, service.ToResponse(game));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GameResponse> Get(Guid id)
    {
        var game = service.GetGame(id);
        return game is null ? NotFound() : Ok(service.ToResponse(game));
    }

    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameResponse> Move(Guid id, MoveRequest request)
    {
        var (game, error) = service.MakeMove(id, request);
        if (game is null) return NotFound();
        return error is null ? Ok(service.ToResponse(game)) : BadRequest(new { message = error, state = service.ToResponse(game) });
    }

    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameResponse> Undo(Guid id)
    {
        var (game, error) = service.Undo(id);
        if (game is null) return NotFound();
        return error is null ? Ok(service.ToResponse(game)) : BadRequest(new { message = error, state = service.ToResponse(game) });
    }

    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameResponse> Reset(Guid id)
    {
        var game = service.Reset(id);
        return game is null ? NotFound() : Ok(service.ToResponse(game));
    }
}
