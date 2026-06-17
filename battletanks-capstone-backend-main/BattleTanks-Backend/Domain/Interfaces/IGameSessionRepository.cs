using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Enums;

namespace BattleTanks_Backend.Domain.Interfaces;

public interface IGameSessionRepository
{
    Task<IEnumerable<GameSession>> GetAllAsync();

    Task<(IEnumerable<GameSession> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize);

    Task<GameSession?> GetByIdAsync(Guid id);

    Task<IEnumerable<GameSession>> GetByStatusAsync(GameStatus status);

    Task<IEnumerable<GameSession>> GetActiveSessionsAsync();

    Task<GameSession> CreateAsync(GameSession session);

    Task<GameSession> UpdateAsync(GameSession session);

    Task<bool> DeleteAsync(Guid id);

    Task<GameSession?> UpdateStatusAsync(Guid id, GameStatus newStatus);
}
