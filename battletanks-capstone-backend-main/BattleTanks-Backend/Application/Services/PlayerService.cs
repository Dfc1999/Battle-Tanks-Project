using BattleTanks_Backend.Application.DTOs.Player;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;
using System.Text.Json;

namespace BattleTanks_Backend.Application.Services;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly RedisCacheService _redisCache;
    private const string AllPlayersCacheKey = "cache:players:all";

    public PlayerService(IPlayerRepository playerRepository, RedisCacheService redisCache)
    {
        _playerRepository = playerRepository;
        _redisCache = redisCache;
    }

    public async Task<IEnumerable<PlayerDto>> GetAllPlayersAsync()
    {
        var db = _redisCache.GetConnection().GetDatabase();

        var cached = await db.StringGetAsync(AllPlayersCacheKey);
        if (cached.HasValue)
        {
            var cachedPlayers = JsonSerializer.Deserialize<List<PlayerDto>>(cached.ToString());
            if (cachedPlayers != null)
                return cachedPlayers;
        }

        var players = await _playerRepository.GetAllAsync();
        var dtos = players.Select(MapToDto).ToList();

        var json = JsonSerializer.Serialize(dtos);
        await db.StringSetAsync(AllPlayersCacheKey, json, TimeSpan.FromSeconds(60));

        return dtos;
    }

    public async Task<PlayerDto?> GetPlayerByIdAsync(Guid id)
    {
        var player = await _playerRepository.GetByIdAsync(id);
        return player != null ? MapToDto(player) : null;
    }

    public async Task<PlayerDto?> GetPlayerByUsernameAsync(string username)
    {
        var player = await _playerRepository.GetByUsernameAsync(username);
        return player != null ? MapToDto(player) : null;
    }

    public async Task<PlayerDto> CreatePlayerAsync(CreatePlayerDto dto)
    {
        if (await _playerRepository.ExistsByUsernameAsync(dto.Username))
        {
            throw new InvalidOperationException($"El username '{dto.Username}' ya está en uso.");
        }

        if (await _playerRepository.ExistsByEmailAsync(dto.Email))
        {
            throw new InvalidOperationException($"El email '{dto.Email}' ya está registrado.");
        }

        var player = new Player
        {
            Username = dto.Username,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow
        };

        var createdPlayer = await _playerRepository.CreateAsync(player);
        await InvalidatePlayersCacheAsync();
        return MapToDto(createdPlayer);
    }

    public async Task<PlayerDto?> UpdatePlayerAsync(Guid id, UpdatePlayerDto dto)
    {
        var player = await _playerRepository.GetByIdAsync(id);
        if (player == null) return null;

        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != player.Username)
        {
            if (await _playerRepository.ExistsByUsernameAsync(dto.Username))
            {
                throw new InvalidOperationException($"El username '{dto.Username}' ya está en uso.");
            }
            player.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != player.Email)
        {
            if (await _playerRepository.ExistsByEmailAsync(dto.Email))
            {
                throw new InvalidOperationException($"El email '{dto.Email}' ya está registrado.");
            }
            player.Email = dto.Email;
        }

        var updatedPlayer = await _playerRepository.UpdateAsync(player);
        await InvalidatePlayersCacheAsync();
        return MapToDto(updatedPlayer);
    }

    public async Task<bool> DeletePlayerAsync(Guid id)
    {
        var result = await _playerRepository.DeleteAsync(id);
        if (result)
            await InvalidatePlayersCacheAsync();
        return result;
    }

    private async Task InvalidatePlayersCacheAsync()
    {
        var db = _redisCache.GetConnection().GetDatabase();
        await db.KeyDeleteAsync(AllPlayersCacheKey);
    }

    private static PlayerDto MapToDto(Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            Username = player.Username,
            Email = player.Email,
            GamesPlayed = player.GamesPlayed,
            Wins = player.Wins,
            TotalScore = player.TotalScore,
            CreatedAt = player.CreatedAt,
            LastLogin = player.LastLogin
        };
    }
}
