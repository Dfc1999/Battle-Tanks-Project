using BattleTanks_Backend.Application.DTOs.GameSession;
using BattleTanks_Backend.Domain.Enums;

namespace BattleTanks_Backend.Application.Interfaces;

public interface IGameSessionService
{
    Task<IEnumerable<GameSessionDto>> GetAllSessionsAsync();
    Task<IEnumerable<GameSessionDto>> GetActiveSessionsAsync();
    Task<GameSessionDto?> GetSessionByIdAsync(Guid id);
    Task<GameSessionDto> CreateSessionAsync(CreateSessionDto dto);
    Task<GameSessionDto?> UpdateSessionStatusAsync(Guid id, GameStatus status);
    Task<bool> DeleteSessionAsync(Guid id);
}
