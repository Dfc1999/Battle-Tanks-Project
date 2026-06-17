using StackExchange.Redis;
using System.Text.Json;

namespace BattleTanksAPI.Services
{
    public class EventHistoryService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly int _maxEventsPerRoom = 50;

        public EventHistoryService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task StoreEventAsync(string roomId, string eventType, object eventData)
        {
            var db = _redis.GetDatabase();
            var key = $"room:{roomId}:events";

            var eventEntry = new
            {
                type = eventType,
                data = eventData,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var json = JsonSerializer.Serialize(eventEntry);

            await db.ListRightPushAsync(key, json);

            await db.ListTrimAsync(key, -_maxEventsPerRoom, -1);

            await db.KeyExpireAsync(key, TimeSpan.FromHours(1));

            Console.WriteLine($"[Redis] Evento {eventType} almacenado en sala {roomId}");
        }

        public async Task<List<string>> GetRoomHistoryAsync(string roomId)
        {
            var db = _redis.GetDatabase();
            var key = $"room:{roomId}:events";

            var events = await db.ListRangeAsync(key);

            Console.WriteLine($"[Redis] Recuperados {events.Length} eventos de sala {roomId}");

            return events.Select(e => e.ToString()).ToList();
        }

        public async Task ClearRoomHistoryAsync(string roomId)
        {
            var db = _redis.GetDatabase();
            var key = $"room:{roomId}:events";
            await db.KeyDeleteAsync(key);
            Console.WriteLine($"[Redis] Historial limpiado para sala {roomId}");
        }
    }
}
