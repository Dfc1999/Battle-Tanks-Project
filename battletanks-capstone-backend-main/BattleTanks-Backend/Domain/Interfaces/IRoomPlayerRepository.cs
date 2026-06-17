using BattleTanks_Backend.Domain.Entities;

namespace BattleTanks_Backend.Domain.Interfaces;

public interface IRoomPlayerRepository
{
    Task<IEnumerable<RoomPlayer>> GetBySessionIdAsync(Guid sessionId);
    Task<RoomPlayer?> GetByPlayerAndSessionAsync(Guid playerId, Guid sessionId);
    Task<int> GetActivePlayerCountAsync(Guid sessionId);
    Task<RoomPlayer> AddPlayerToRoomAsync(RoomPlayer roomPlayer);
    Task<bool> RemovePlayerFromRoomAsync(Guid playerId, Guid sessionId);
    Task<bool> IsPlayerInAnyRoomAsync(Guid playerId);
}
