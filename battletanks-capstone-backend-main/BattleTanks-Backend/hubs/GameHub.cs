using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BattleTanksAPI.Services;
using BattleTanks_Backend.Application.Services;

// Evidencia Actividad 1 - Laboratorio 6

namespace BattleTanksAPI.Hubs
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HubPlayer>> _rooms = new();
        private static readonly ConcurrentDictionary<string, string> _connectionRooms = new();
        private static readonly ConcurrentDictionary<string, HashSet<string>> _eliminatedPlayers = new();
        private static readonly ConcurrentDictionary<string, long> _lastMoveTimestamp = new();
        private const long MoveThrottleMs = 50;

        private readonly EventHistoryService _eventHistory;
        private readonly RedisCacheService _redisCache;

        public GameHub(EventHistoryService eventHistory, RedisCacheService redisCache)
        {
            _eventHistory = eventHistory;
            _redisCache = redisCache;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var roomId = httpContext?.Request.Query["roomId"].ToString() ?? "default-room";
            var playerId = httpContext?.Request.Query["playerId"].ToString() ?? Guid.NewGuid().ToString();
            var playerName = httpContext?.Request.Query["playerName"].ToString() ?? "Player";

            Console.WriteLine($"Nueva conexion: {playerName} (ID: {playerId}) en sala {roomId}");

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            _connectionRooms[Context.ConnectionId] = roomId;

            _rooms.TryAdd(roomId, new ConcurrentDictionary<string, HubPlayer>());

            var player = new HubPlayer
            {
                Id = playerId,
                Name = playerName,
                IsReady = false,
                ConnectionId = Context.ConnectionId
            };

            _rooms[roomId].TryAdd(playerId, player);

            await _redisCache.AddConnectedPlayerAsync(roomId, playerId, playerName);

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "PLAYER_JOINED",
                Data = player
            });

            var currentPlayers = _rooms[roomId].Values;
            await Clients.Caller.SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "CURRENT_PLAYERS",
                Data = currentPlayers
            });

            await base.OnConnectedAsync();

            var history = await _eventHistory.GetRoomHistoryAsync(roomId);
            if (history.Count > 0)
            {
                Console.WriteLine($"[Redis] Enviando {history.Count} eventos historicos a {playerName}");
                await Clients.Caller.SendAsync("ReceiveEventHistory", history);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionRooms.TryGetValue(Context.ConnectionId, out var roomId))
            {
                if (_rooms.TryGetValue(roomId, out var players))
                {
                    var player = players.Values.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

                    if (player != null)
                    {
                        players.TryRemove(player.Id, out _);

                        await _redisCache.RemoveConnectedPlayerAsync(roomId, player.Id);

                        Console.WriteLine($"Desconexion: {player.Name} de la sala {roomId}");

                        await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
                        {
                            Type = "PLAYER_LEFT",
                            Data = new { PlayerId = player.Id, PlayerName = player.Name }
                        });
                    }
                }

                _connectionRooms.TryRemove(Context.ConnectionId, out _);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(GameMessage message)
        {
            if (!_connectionRooms.TryGetValue(Context.ConnectionId, out var roomId))
            {
                return;
            }

            switch (message.Type)
            {
                case "PLAYER_MOVE":
                    await HandlePlayerMove(roomId, message.Data);
                    break;

                case "CHAT_MESSAGE":
                    await HandleChatMessage(roomId, message.Data);
                    break;

                case "PLAYER_READY":
                    await HandlePlayerReady(roomId, message.Data);
                    break;

                case "START_GAME":
                    await HandleStartGame(roomId);
                    break;

                case "PLAYER_HIT":
                    await HandlePlayerHit(roomId, message.Data);
                    break;

                case "TANK_SELECTED":
                    await HandleTankSelected(roomId, message.Data);
                    break;

                case "BULLET_FIRED":
                    await Clients.OthersInGroup(roomId).SendAsync("ReceiveMessage", new GameMessage
                    {
                        Type = "BULLET_FIRED",
                        Data = message.Data
                    });
                    break;

                // Eventos de baja latencia Power-ups
                case "POWER_UP_SPAWNED":
                    await HandlePowerUpSpawned(roomId, message.Data);
                    break;

                case "COLLECT_POWER_UP":
                    await HandleCollectPowerUp(roomId, message.Data);
                    break;

                case "GAME_OVER":
                    await HandleGameOver(roomId, message.Data);
                    break;

                default:
                break;
            }
        }

        private async Task HandlePlayerMove(string roomId, object? data)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var connId = Context.ConnectionId;

            if (_lastMoveTimestamp.TryGetValue(connId, out var lastTs) && (now - lastTs) < MoveThrottleMs)
                return;

            _lastMoveTimestamp[connId] = now;

            await Clients.OthersInGroup(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "PLAYER_MOVE",
                Data = data
            });
        }

        private async Task HandleChatMessage(string roomId, object? data)
        {
            Console.WriteLine($"Mensaje de chat en sala {roomId}");

            // Almacenar chat en Redis
            await _eventHistory.StoreEventAsync(roomId, "CHAT_MESSAGE", data!);

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "CHAT_MESSAGE",
                Data = data
            });
        }

        private async Task HandlePlayerReady(string roomId, object? data)
        {
            if (data is System.Text.Json.JsonElement jsonData)
            {
                var playerId = jsonData.GetProperty("playerId").GetString();
                var isReady = jsonData.GetProperty("isReady").GetBoolean();

                if (_rooms.TryGetValue(roomId, out var players) && playerId != null)
                {
                    if (players.TryGetValue(playerId, out var player))
                    {
                        player.IsReady = isReady;
                        Console.WriteLine($"Jugador {player.Name} esta {(isReady ? "LISTO" : "NO listo")} en sala {roomId}");
                    }
                }
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "PLAYER_READY",
                Data = data
            });

            await CheckAllPlayersReady(roomId);
        }

        private async Task CheckAllPlayersReady(string roomId)
        {
            if (!_rooms.TryGetValue(roomId, out var players)) return;
            if (players.Count < 2) return;

            var allReady = players.Values.All(p => p.IsReady);
            if (allReady)
            {
                Console.WriteLine($"Todos los jugadores en sala {roomId} estan listos!");
            }
        }

        private async Task HandleStartGame(string roomId)
        {
            Console.WriteLine($"Iniciando juego en sala {roomId}!");

            var players = _rooms.TryGetValue(roomId, out var p) ? p.Values.ToList() : new List<HubPlayer>();

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "GAME_STARTED",
                Data = new { RoomId = roomId, Players = players }
            });
        }

        // Evento de colision con sentTimestamp para benchmarking
        private async Task HandlePlayerHit(string roomId, object? data)
        {
            Console.WriteLine($"[Benchmark] Impacto registrado en sala {roomId} - timestamp inyectado por servidor");

            var serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            object enrichedData = data!;
            if (data is System.Text.Json.JsonElement hitJson)
            {
                enrichedData = new
                {
                    targetPlayerId = hitJson.TryGetProperty("targetPlayerId", out var t) ? t.GetString() : "",
                    shooterId = hitJson.TryGetProperty("shooterId", out var s) ? s.GetString() : "",
                    damage = hitJson.TryGetProperty("damage", out var d) ? d.GetInt32() : 1,
                    sentTimestamp = serverTimestamp
                };
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "PLAYER_HIT",
                Data = enrichedData
            });

            // Almacenar en Redis
            await _eventHistory.StoreEventAsync(roomId, "PLAYER_HIT", enrichedData);
        }

        // Evento de power-up con sentTimestamp para benchmarking
        private async Task HandlePowerUpSpawned(string roomId, object? data)
        {
            Console.WriteLine($"[Benchmark] Power-up generado en sala {roomId}");

            var serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            object enrichedData = data!;
            if (data is System.Text.Json.JsonElement puJson)
            {
                enrichedData = new
                {
                    id = puJson.TryGetProperty("id", out var id) ? id.GetString() : Guid.NewGuid().ToString(),
                    x = puJson.TryGetProperty("x", out var x) ? x.GetDouble() : 0,
                    y = puJson.TryGetProperty("y", out var y) ? y.GetDouble() : 0,
                    type = puJson.TryGetProperty("type", out var tp) ? tp.GetString() : "health",
                    sentTimestamp = serverTimestamp
                };
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "POWER_UP_SPAWNED",
                Data = enrichedData
            });

            // Almacenar en Redis
            await _eventHistory.StoreEventAsync(roomId, "POWER_UP_SPAWNED", enrichedData);
        }

        // Recoleccion de power-up con sentTimestamp
        private async Task HandleCollectPowerUp(string roomId, object? data)
        {
            Console.WriteLine($"[Benchmark] Power-up recolectado en sala {roomId}");

            var serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            object enrichedData = data!;
            if (data is System.Text.Json.JsonElement collectJson)
            {
                enrichedData = new
                {
                    powerUpId = collectJson.TryGetProperty("powerUpId", out var pid) ? pid.GetString() : "",
                    playerId = collectJson.TryGetProperty("playerId", out var plid) ? plid.GetString() : "",
                    type = collectJson.TryGetProperty("type", out var tp) ? tp.GetString() : "health",
                    sentTimestamp = serverTimestamp
                };
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "COLLECT_POWER_UP",
                Data = enrichedData
            });

            // Almacenar en Redis
            await _eventHistory.StoreEventAsync(roomId, "COLLECT_POWER_UP", enrichedData);
        }

        private async Task HandleGameOver(string roomId, object? data)
        {
            Console.WriteLine($"Game Over en sala {roomId}");

            var serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var eliminatedId = "";
            var eliminatedBy = "";

            if (data is System.Text.Json.JsonElement goJson)
            {
                eliminatedId = goJson.TryGetProperty("eliminatedPlayerId", out var ep) ? ep.GetString() ?? "" : "";
                eliminatedBy = goJson.TryGetProperty("eliminatedBy", out var eb) ? eb.GetString() ?? "" : "";
            }

            var enrichedData = new
            {
                eliminatedPlayerId = eliminatedId,
                eliminatedBy = eliminatedBy,
                sentTimestamp = serverTimestamp
            };

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "GAME_OVER",
                Data = enrichedData
            });

            await _eventHistory.StoreEventAsync(roomId, "GAME_OVER", enrichedData);

            // Rastrear jugador eliminado
            if (!string.IsNullOrEmpty(eliminatedId))
            {
                _eliminatedPlayers.TryAdd(roomId, new HashSet<string>());
                _eliminatedPlayers[roomId].Add(eliminatedId);

                await CheckMatchEnded(roomId);
            }
        }

        private async Task CheckMatchEnded(string roomId)
        {
            if (!_rooms.TryGetValue(roomId, out var players)) return;
            if (!_eliminatedPlayers.TryGetValue(roomId, out var eliminated)) return;

            var alivePlayers = players.Values
                .Where(p => !eliminated.Contains(p.Id))
                .ToList();

            // La partida termina cuando queda solo 1 jugador vivo (y hay al menos 2 en la sala)
            if (alivePlayers.Count == 1 && players.Count >= 2)
            {
                var winner = alivePlayers.First();
                Console.WriteLine($"MATCH_ENDED en sala {roomId} - Ganador: {winner.Name}");

                var playerScores = players.Values.Select(p => new
                {
                    playerId = p.Id,
                    playerName = p.Name,
                    isWinner = p.Id == winner.Id
                }).ToList();

                await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
                {
                    Type = "MATCH_ENDED",
                    Data = new
                    {
                        roomId = roomId,
                        winnerId = winner.Id,
                        winnerName = winner.Name,
                        players = playerScores
                    }
                });

                // Limpiar estado de eliminados para la sala
                _eliminatedPlayers.TryRemove(roomId, out _);
            }
        }

        private async Task HandleTankSelected(string roomId, object? data)
        {
            if (data is System.Text.Json.JsonElement jsonData)
            {
                var playerId = jsonData.GetProperty("playerId").GetString();
                var tankType = jsonData.GetProperty("tankType").GetString();

                if (_rooms.TryGetValue(roomId, out var players) && playerId != null)
                {
                    if (players.TryGetValue(playerId, out var player))
                    {
                        player.TankType = tankType ?? "e100";
                        Console.WriteLine($"Jugador {player.Name} selecciono tanque: {tankType} en sala {roomId}");
                    }
                }
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new GameMessage
            {
                Type = "TANK_SELECTED",
                Data = data
            });
        }

        public async Task GetRoomPlayers(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var players))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new GameMessage
                {
                    Type = "ROOM_PLAYERS",
                    Data = players.Values
                });
            }
        }

        public async Task DestroyObstacle(string obstacleId)
        {
            if (_connectionRooms.TryGetValue(Context.ConnectionId, out var roomId))
            {
                Console.WriteLine($"Obstaculo destruido en sala {roomId}: {obstacleId}");

                await Clients.OthersInGroup(roomId).SendAsync("ReceiveMessage", new GameMessage
                {
                    Type = "OBSTACLE_DESTROYED",
                    Data = obstacleId
                });
            }
        }
    }
}
