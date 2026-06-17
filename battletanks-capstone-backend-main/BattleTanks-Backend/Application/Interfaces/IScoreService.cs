using BattleTanks_Backend.Application.DTOs.Score;

namespace BattleTanks_Backend.Application.Interfaces;

public interface IScoreService
{
    Task<IEnumerable<ScoreDto>> GetAllScoresAsync();
    Task<ScoreDto?> GetScoreByIdAsync(Guid id);
    Task<IEnumerable<ScoreDto>> GetScoresByPlayerIdAsync(Guid playerId);
    Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int top = 10);
    Task<ScoreDto> CreateScoreAsync(CreateScoreDto dto);
    Task<bool> DeleteScoreAsync(Guid id);
}
