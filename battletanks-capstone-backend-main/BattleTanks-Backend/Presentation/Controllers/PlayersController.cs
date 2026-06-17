using Microsoft.AspNetCore.Mvc;
using BattleTanks_Backend.Application.DTOs.Player;
using BattleTanks_Backend.Application.Interfaces;

namespace BattleTanks_Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlayerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlayerDto>>> GetAll()
    {
        var players = await _playerService.GetAllPlayersAsync();
        return Ok(players);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDto>> GetById(Guid id)
    {
        var player = await _playerService.GetPlayerByIdAsync(id);

        if (player == null)
            return NotFound(new { message = $"Jugador con ID '{id}' no encontrado." });

        return Ok(player);
    }

    [HttpGet("username/{username}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDto>> GetByUsername(string username)
    {
        var player = await _playerService.GetPlayerByUsernameAsync(username);

        if (player == null)
            return NotFound(new { message = $"Jugador '{username}' no encontrado." });

        return Ok(player);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerDto>> Create([FromBody] CreatePlayerDto dto)
    {
        try
        {
            var player = await _playerService.CreatePlayerAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = player.Id }, player);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerDto>> Update(Guid id, [FromBody] UpdatePlayerDto dto)
    {
        try
        {
            var player = await _playerService.UpdatePlayerAsync(id, dto);

            if (player == null)
                return NotFound(new { message = $"Jugador con ID '{id}' no encontrado." });

            return Ok(player);
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
        var deleted = await _playerService.DeletePlayerAsync(id);

        if (!deleted)
            return NotFound(new { message = $"Jugador con ID '{id}' no encontrado." });

        return NoContent();
    }
}
