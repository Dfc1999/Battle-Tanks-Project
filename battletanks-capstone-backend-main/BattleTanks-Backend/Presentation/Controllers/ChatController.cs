using Microsoft.AspNetCore.Mvc;
using BattleTanks_Backend.Application.DTOs.Chat;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Infrastructure.Middleware;

namespace BattleTanks_Backend.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("{roomId}")]
    [ProducesResponseType(typeof(ChatHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatHistoryResponse>> GetChatHistory(Guid roomId, [FromQuery] int limit = 50)
    {
        try
        {
            var history = await _chatService.GetChatHistoryAsync(roomId, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [RequireFirebaseAuth]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChatMessageDto>> SendMessage([FromBody] CreateChatMessageDto dto)
    {
        try
        {
            var playerId = GetPlayerIdFromContext();
            if (playerId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            Console.WriteLine($"[Chat] Sending message from player {playerId} to room {dto.RoomId}");
            var message = await _chatService.SendMessageAsync(playerId.Value, dto);
            Console.WriteLine($"[Chat] Message saved successfully: {message.Id}");
            return CreatedAtAction(nameof(GetChatHistory), new { roomId = dto.RoomId }, message);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[Chat] InvalidOperationException: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Chat] Exception: {ex.Message}");
            Console.WriteLine($"[Chat] Stack: {ex.StackTrace}");
            return BadRequest(new { message = "Error sending message", details = ex.Message });
        }
    }

    private Guid? GetPlayerIdFromContext()
    {
        if (HttpContext.Items.TryGetValue("PlayerId", out var playerIdObj) && playerIdObj is Guid playerId)
        {
            return playerId;
        }
        return null;
    }
}
