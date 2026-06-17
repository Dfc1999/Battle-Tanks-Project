using BattleTanks_Backend.Domain.Entities;

namespace BattleTanks_Backend.Domain.Interfaces;

public interface IScoreRepository
{
    Task<IEnumerable<Score>> GetAllAsync();

    Task<(IEnumerable<Score> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize);

    Task<Score?> GetByIdAsync(Guid id);

    Task<IEnumerable<Score>> GetByPlayerIdAsync(Guid playerId);

    Task<(IEnumerable<Score> Items, int TotalCount)> GetByPlayerIdPaginatedAsync(Guid playerId, int page, int pageSize);

    Task<IEnumerable<Score>> GetBySessionIdAsync(Guid sessionId);

    Task<IEnumerable<Score>> GetLeaderboardAsync(int top = 10);

    Task<Score> CreateAsync(Score score);

    Task<Score> UpdateAsync(Score score);

    Task<bool> DeleteAsync(Guid id);

    Task BulkInsertScoresAsync(IList<Score> scores);
}
