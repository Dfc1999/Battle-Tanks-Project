using Microsoft.AspNetCore.Mvc;
using BattleTanks_Backend.Application.DTOs.GameSession;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Domain.Enums;

namespace BattleTanks_Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GameSessionsController : ControllerBase
{
    private readonly IGameSessionService _sessionService;

    public GameSessionsController(IGameSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GameSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GameSessionDto>>> GetAll()
    {
        var sessions = await _sessionService.GetAllSessionsAsync();
        return Ok(sessions);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<GameSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GameSessionDto>>> GetActive()
    {
        var sessions = await _sessionService.GetActiveSessionsAsync();
        return Ok(sessions);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameSessionDto>> GetById(Guid id)
    {
        var session = await _sessionService.GetSessionByIdAsync(id);

        if (session == null)
            return NotFound(new { message = $"Sesión con ID '{id}' no encontrada." });

        return Ok(session);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GameSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameSessionDto>> Create([FromBody] CreateSessionDto dto)
    {
        var session = await _sessionService.CreateSessionAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(GameSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameSessionDto>> UpdateStatus(Guid id, [FromBody] UpdateSessionStatusDto dto)
    {
        var session = await _sessionService.UpdateSessionStatusAsync(id, dto.Status);

        if (session == null)
            return NotFound(new { message = $"Sesión con ID '{id}' no encontrada." });

        return Ok(session);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _sessionService.DeleteSessionAsync(id);

        if (!deleted)
            return NotFound(new { message = $"Sesión con ID '{id}' no encontrada." });

        return NoContent();
    }
}
