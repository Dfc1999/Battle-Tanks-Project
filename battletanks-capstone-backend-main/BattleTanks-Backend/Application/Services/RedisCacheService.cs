using StackExchange.Redis;
using System.Text.Json;

namespace BattleTanks_Backend.Application.Services;

public class RedisCacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public IConnectionMultiplexer GetConnection() => _redis;

    public async Task AddConnectedPlayerAsync(string roomId, string playerId, string playerName)
    {
        var db = _redis.GetDatabase();
        var key = $"room:{roomId}:connected_players";

        var playerData = JsonSerializer.Serialize(new
        {
            playerId,
            playerName,
            connectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        await db.HashSetAsync(key, playerId, playerData);
        await db.KeyExpireAsync(key, TimeSpan.FromHours(2));

        Console.WriteLine($"[Redis Cache] Jugador {playerName} agregado a sala {roomId}");
    }

    public async Task RemoveConnectedPlayerAsync(string roomId, string playerId)
    {
        var db = _redis.GetDatabase();
        var key = $"room:{roomId}:connected_players";

        await db.HashDeleteAsync(key, playerId);

        Console.WriteLine($"[Redis Cache] Jugador {playerId} removido de sala {roomId}");
    }

    public async Task<Dictionary<string, string>> GetConnectedPlayersAsync(string roomId)
    {
        var db = _redis.GetDatabase();
        var key = $"room:{roomId}:connected_players";

        var entries = await db.HashGetAllAsync(key);

        return entries.ToDictionary(
            e => e.Name.ToString(),
            e => e.Value.ToString()
        );
    }

    public async Task<long> GetConnectedPlayerCountAsync(string roomId)
    {
        var db = _redis.GetDatabase();
        var key = $"room:{roomId}:connected_players";

        return await db.HashLengthAsync(key);
    }

    public async Task CacheLeaderboardAsync(string leaderboardJson, int top = 10)
    {
        var db = _redis.GetDatabase();
        var key = $"cache:leaderboard:top{top}";

        await db.StringSetAsync(key, leaderboardJson, TimeSpan.FromSeconds(60));

        Console.WriteLine($"[Redis Cache] Leaderboard top {top} cacheado por 60 segundos");
    }

    public async Task<string?> GetCachedLeaderboardAsync(int top = 10)
    {
        var db = _redis.GetDatabase();
        var key = $"cache:leaderboard:top{top}";

        var cached = await db.StringGetAsync(key);

        if (cached.HasValue)
        {
            Console.WriteLine($"[Redis Cache] Leaderboard top {top} servido desde caché");
            return cached.ToString();
        }

        Console.WriteLine($"[Redis Cache] Leaderboard top {top} no encontrado en caché");
        return null;
    }

    public async Task InvalidateLeaderboardCacheAsync()
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        var keys = server.Keys(pattern: "cache:leaderboard:*").ToArray();
        foreach (var key in keys)
        {
            await db.KeyDeleteAsync(key);
        }

        Console.WriteLine("[Redis Cache] Caché de leaderboard invalidado");
    }

    public async Task CacheSessionAsync(string firebaseUid, Dictionary<string, string> sessionData)
    {
        var db = _redis.GetDatabase();
        var key = $"session:{firebaseUid}";

        var entries = sessionData.Select(kvp =>
            new HashEntry(kvp.Key, kvp.Value)).ToArray();

        await db.HashSetAsync(key, entries);
        await db.KeyExpireAsync(key, TimeSpan.FromMinutes(30));

        Console.WriteLine($"[Redis Cache] Sesión cacheada para UID {firebaseUid} (TTL: 30min)");
    }

    public async Task<Dictionary<string, string>?> GetCachedSessionAsync(string firebaseUid)
    {
        var db = _redis.GetDatabase();
        var key = $"session:{firebaseUid}";

        var entries = await db.HashGetAllAsync(key);

        if (entries.Length == 0)
        {
            Console.WriteLine($"[Redis Cache] Sesión no encontrada para UID {firebaseUid}");
            return null;
        }

        Console.WriteLine($"[Redis Cache] Sesión servida desde caché para UID {firebaseUid}");
        return entries.ToDictionary(
            e => e.Name.ToString(),
            e => e.Value.ToString()
        );
    }

    public async Task InvalidateSessionAsync(string firebaseUid)
    {
        var db = _redis.GetDatabase();
        var key = $"session:{firebaseUid}";

        await db.KeyDeleteAsync(key);

        Console.WriteLine($"[Redis Cache] Sesión invalidada para UID {firebaseUid}");
    }
}
