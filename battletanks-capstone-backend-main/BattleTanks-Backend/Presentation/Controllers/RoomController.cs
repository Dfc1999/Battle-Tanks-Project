using Microsoft.AspNetCore.Mvc;
using BattleTanks_Backend.Application.DTOs.Room;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Infrastructure.Middleware;

namespace BattleTanks_Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ICurrentUserService _currentUser;

    public RoomController(IRoomService roomService, ICurrentUserService currentUser)
    {
        _roomService = roomService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAvailableRooms()
    {
        var rooms = await _roomService.GetAvailableRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetById(Guid id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);

        if (room == null)
            return NotFound(new { message = "Sala no encontrada" });

        return Ok(room);
    }

    [HttpPost]
    [RequireFirebaseAuth]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomDto dto)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.PlayerId.HasValue)
        {
            return Unauthorized(new { message = "Debes estar autenticado para crear una sala" });
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "El nombre de la sala es requerido" });
        }

        var room = await _roomService.CreateRoomAsync(dto, _currentUser.PlayerId.Value);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpPut("{id:guid}/join")]
    [RequireFirebaseAuth]
    [ProducesResponseType(typeof(JoinRoomResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JoinRoomResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JoinRoomResponseDto>> JoinRoom(Guid id)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.PlayerId.HasValue)
        {
            return Unauthorized(new { message = "Debes estar autenticado para unirte a una sala" });
        }

        var result = await _roomService.JoinRoomAsync(id, _currentUser.PlayerId.Value);

        if (!result.Success)
        {
            if (result.Message.Contains("no encontrada"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("{id:guid}/leave")]
    [RequireFirebaseAuth]
    [ProducesResponseType(typeof(JoinRoomResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JoinRoomResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JoinRoomResponseDto>> LeaveRoom(Guid id)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.PlayerId.HasValue)
        {
            return Unauthorized(new { message = "Debes estar autenticado para salir de una sala" });
        }

        var result = await _roomService.LeaveRoomAsync(id, _currentUser.PlayerId.Value);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}

