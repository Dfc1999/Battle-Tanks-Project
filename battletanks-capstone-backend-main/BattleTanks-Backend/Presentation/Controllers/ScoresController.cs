using Microsoft.AspNetCore.Mvc;
using BattleTanks_Backend.Application.DTOs.Score;
using BattleTanks_Backend.Application.Interfaces;

namespace BattleTanks_Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ScoresController : ControllerBase
{
    private readonly IScoreService _scoreService;

    public ScoresController(IScoreService scoreService)
    {
        _scoreService = scoreService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ScoreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScoreDto>>> GetAll()
    {
        var scores = await _scoreService.GetAllScoresAsync();
        return Ok(scores);
    }

    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(IEnumerable<LeaderboardEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard([FromQuery] int top = 10)
    {
        var leaderboard = await _scoreService.GetLeaderboardAsync(top);
        return Ok(leaderboard);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ScoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScoreDto>> GetById(Guid id)
    {
        var score = await _scoreService.GetScoreByIdAsync(id);

        if (score == null)
            return NotFound(new { message = $"Puntaje con ID '{id}' no encontrado." });

        return Ok(score);
    }

    [HttpGet("player/{playerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ScoreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScoreDto>>> GetByPlayerId(Guid playerId)
    {
        var scores = await _scoreService.GetScoresByPlayerIdAsync(playerId);
        return Ok(scores);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ScoreDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScoreDto>> Create([FromBody] CreateScoreDto dto)
    {
        try
        {
            var score = await _scoreService.CreateScoreAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = score.Id }, score);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _scoreService.DeleteScoreAsync(id);

        if (!deleted)
            return NotFound(new { message = $"Puntaje con ID '{id}' no encontrado." });

        return NoContent();
    }
}
