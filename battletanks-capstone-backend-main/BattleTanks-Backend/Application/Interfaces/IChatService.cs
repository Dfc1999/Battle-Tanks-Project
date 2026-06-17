using BattleTanks_Backend.Application.DTOs.Chat;

namespace BattleTanks_Backend.Application.Interfaces;

public interface IChatService
{
    Task<ChatHistoryResponse> GetChatHistoryAsync(Guid roomId, int limit = 50);
    Task<ChatMessageDto> SendMessageAsync(Guid playerId, CreateChatMessageDto dto);
}
