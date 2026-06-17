using BattleTanks_Backend.Application.DTOs.GameSession;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Enums;
using BattleTanks_Backend.Domain.Interfaces;

namespace BattleTanks_Backend.Application.Services;

public class GameSessionService : IGameSessionService
{
    private readonly IGameSessionRepository _sessionRepository;

    public GameSessionService(IGameSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<IEnumerable<GameSessionDto>> GetAllSessionsAsync()
    {
        var sessions = await _sessionRepository.GetAllAsync();
        return sessions.Select(MapToDto);
    }

    public async Task<IEnumerable<GameSessionDto>> GetActiveSessionsAsync()
    {
        var sessions = await _sessionRepository.GetActiveSessionsAsync();
        return sessions.Select(MapToDto);
    }

    public async Task<GameSessionDto?> GetSessionByIdAsync(Guid id)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        return session != null ? MapToDto(session) : null;
    }

    public async Task<GameSessionDto> CreateSessionAsync(CreateSessionDto dto)
    {
        var session = new GameSession
        {
            Name = dto.Name,
            SelectedMap = dto.SelectedMap,
            MaxPlayers = dto.MaxPlayers,
            Status = GameStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };

        var createdSession = await _sessionRepository.CreateAsync(session);
        return MapToDto(createdSession);
    }

    public async Task<GameSessionDto?> UpdateSessionStatusAsync(Guid id, GameStatus status)
    {
        var session = await _sessionRepository.UpdateStatusAsync(id, status);
        return session != null ? MapToDto(session) : null;
    }

    public async Task<bool> DeleteSessionAsync(Guid id)
    {
        return await _sessionRepository.DeleteAsync(id);
    }

    private static GameSessionDto MapToDto(GameSession session)
    {
        return new GameSessionDto
        {
            Id = session.Id,
            Name = session.Name,
            SelectedMap = session.SelectedMap,
            Status = session.Status.ToString(),
            MaxPlayers = session.MaxPlayers,
            CurrentPlayers = session.Scores?.Select(s => s.PlayerId).Distinct().Count() ?? 0,
            CreatedAt = session.CreatedAt,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt
        };
    }
}
