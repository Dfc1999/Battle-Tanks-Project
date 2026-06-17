using BattleTanks_Backend.Domain.Entities;

namespace BattleTanks_Backend.Domain.Interfaces;

public interface IChatRepository
{
    Task<List<ChatMessage>> GetMessagesByRoomIdAsync(Guid roomId, int limit = 50);
    Task<ChatMessage> CreateMessageAsync(ChatMessage message);
    Task<int> GetMessageCountByRoomIdAsync(Guid roomId);
}
