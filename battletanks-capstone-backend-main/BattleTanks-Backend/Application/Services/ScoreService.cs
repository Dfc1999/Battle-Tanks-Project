using BattleTanks_Backend.Application.DTOs.Score;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;
using System.Text.Json;

namespace BattleTanks_Backend.Application.Services;

public class ScoreService : IScoreService
{
    private readonly IScoreRepository _scoreRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly RedisCacheService _redisCache;

    public ScoreService(
        IScoreRepository scoreRepository,
        IPlayerRepository playerRepository,
        IGameSessionRepository sessionRepository,
        RedisCacheService redisCache)
    {
        _scoreRepository = scoreRepository;
        _playerRepository = playerRepository;
        _sessionRepository = sessionRepository;
        _redisCache = redisCache;
    }

    public async Task<IEnumerable<ScoreDto>> GetAllScoresAsync()
    {
        var scores = await _scoreRepository.GetAllAsync();
        return scores.Select(MapToDto);
    }

    public async Task<ScoreDto?> GetScoreByIdAsync(Guid id)
    {
        var score = await _scoreRepository.GetByIdAsync(id);
        return score != null ? MapToDto(score) : null;
    }

    public async Task<IEnumerable<ScoreDto>> GetScoresByPlayerIdAsync(Guid playerId)
    {
        var scores = await _scoreRepository.GetByPlayerIdAsync(playerId);
        return scores.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int top = 10)
    {
        var cachedJson = await _redisCache.GetCachedLeaderboardAsync(top);
        if (cachedJson != null)
        {
            var cached = JsonSerializer.Deserialize<List<LeaderboardEntryDto>>(cachedJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cached != null) return cached;
        }

        var scores = await _scoreRepository.GetLeaderboardAsync(top);

        var leaderboard = scores.Select((score, index) => new LeaderboardEntryDto
        {
            Rank = index + 1,
            PlayerName = score.Player?.Username ?? "Unknown",
            Points = score.Points,
            AchievedAt = score.AchievedAt
        }).ToList();

        var json = JsonSerializer.Serialize(leaderboard);
        await _redisCache.CacheLeaderboardAsync(json, top);

        return leaderboard;
    }

    public async Task<ScoreDto> CreateScoreAsync(CreateScoreDto dto)
    {
        var player = await _playerRepository.GetByIdAsync(dto.PlayerId);
        if (player == null)
        {
            throw new InvalidOperationException($"Jugador con ID '{dto.PlayerId}' no encontrado.");
        }

        var session = await _sessionRepository.GetByIdAsync(dto.SessionId);
        if (session == null)
        {
            throw new InvalidOperationException($"Sesión con ID '{dto.SessionId}' no encontrada.");
        }

        var score = new Score
        {
            PlayerId = dto.PlayerId,
            SessionId = dto.SessionId,
            Points = dto.Points,
            AchievedAt = DateTime.UtcNow
        };

        var createdScore = await _scoreRepository.CreateAsync(score);

        await _redisCache.InvalidateLeaderboardCacheAsync();

        createdScore.Player = player;
        createdScore.Session = session;

        return MapToDto(createdScore);
    }

    public async Task<bool> DeleteScoreAsync(Guid id)
    {
        return await _scoreRepository.DeleteAsync(id);
    }

    private static ScoreDto MapToDto(Score score)
    {
        return new ScoreDto
        {
            Id = score.Id,
            PlayerId = score.PlayerId,
            PlayerName = score.Player?.Username ?? "Unknown",
            SessionId = score.SessionId,
            SessionName = score.Session?.Name ?? "Unknown",
            Points = score.Points,
            AchievedAt = score.AchievedAt
        };
    }
}
