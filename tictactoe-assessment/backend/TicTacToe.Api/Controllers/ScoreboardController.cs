using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController(GameService service) : ControllerBase
{
    [HttpGet]
    public ActionResult<Scoreboard> Get() => Ok(service.GetScoreboard());

    [HttpPost("reset")]
    public ActionResult<Scoreboard> Reset()
    {
        service.ResetScoreboard();
        return Ok(service.GetScoreboard());
    }
}
