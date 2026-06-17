using Microsoft.AspNetCore.Mvc;
using BattleTanks_Backend.Application.DTOs.Auth;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Infrastructure.Middleware;

namespace BattleTanks_Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpGet("me")]
    [RequireFirebaseAuth]
    [ProducesResponseType(typeof(PlayerAuthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PlayerAuthDto>> GetCurrentUser()
    {
        var firebaseUid = User.FindFirst("FirebaseUid")?.Value;

        if (string.IsNullOrEmpty(firebaseUid))
        {
            return Unauthorized(new { message = "No autenticado" });
        }

        var player = await _authService.GetCurrentPlayerAsync(firebaseUid);

        if (player == null)
        {
            return NotFound(new { message = "Perfil no encontrado" });
        }

        return Ok(player);
    }
}
