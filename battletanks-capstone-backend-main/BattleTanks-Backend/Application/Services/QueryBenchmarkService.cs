using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using BattleTanks_Backend.Application.DTOs.Benchmark;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Infrastructure.Data;
using System.Text.Json;

namespace BattleTanks_Backend.Application.Services;

public class QueryBenchmarkService
{
    private readonly BattleTanksDbContext _context;
    private readonly IPlayerRepository _playerRepository;
    private readonly IScoreRepository _scoreRepository;
    private readonly RedisCacheService _redisCache;

    public QueryBenchmarkService(
        BattleTanksDbContext context,
        IPlayerRepository playerRepository,
        IScoreRepository scoreRepository,
        RedisCacheService redisCache)
    {
        _context = context;
        _playerRepository = playerRepository;
        _scoreRepository = scoreRepository;
        _redisCache = redisCache;
    }

    public async Task<BenchmarkResultDto> RunAllBenchmarksAsync()
    {
        var result = new BenchmarkResultDto
        {
            ExecutedAt = DateTime.UtcNow,
            Tests = new List<BenchmarkTestDto>()
        };

        var test1 = await BenchmarkTrackingVsNoTrackingAsync();
        result.Tests.Add(test1);

        var test2 = await BenchmarkIndividualVsBulkInsertAsync();
        result.Tests.Add(test2);

        var test3 = await BenchmarkIndexedQueryAsync();
        result.Tests.Add(test3);

        var test4 = await BenchmarkPaginationVsFullLoadAsync();
        result.Tests.Add(test4);

        var test5 = await BenchmarkIndividualVsBulkUpdateAsync();
        result.Tests.Add(test5);

        result.Summary = GenerateSummary(result.Tests);

        return result;
    }

    public async Task<BenchmarkResultDto> RunRedisBenchmarksAsync()
    {
        var result = new BenchmarkResultDto
        {
            ExecutedAt = DateTime.UtcNow,
            Tests = new List<BenchmarkTestDto>()
        };

        var test1 = await BenchmarkRedisVsPostgresLeaderboardAsync();
        result.Tests.Add(test1);

        var test2 = await BenchmarkRedisVsPostgresSessionAsync();
        result.Tests.Add(test2);

        result.Summary = GenerateSummary(result.Tests);

        return result;
    }

    private async Task<BenchmarkTestDto> BenchmarkTrackingVsNoTrackingAsync()
    {
        const int iterations = 5;
        var sw = new Stopwatch();

        var trackingTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var players = await _playerRepository.GetAllWithTrackingAsync();
            sw.Stop();
            trackingTimes.Add(sw.Elapsed.TotalMilliseconds);

            _context.ChangeTracker.Clear();
        }

        var noTrackingTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var players = await _playerRepository.GetAllWithoutTrackingAsync();
            sw.Stop();
            noTrackingTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avgTracking = trackingTimes.Average();
        var avgNoTracking = noTrackingTimes.Average();
        var improvement = avgTracking > 0 ? ((avgTracking - avgNoTracking) / avgTracking) * 100 : 0;

        var recordCount = await _context.Players.CountAsync();

        return new BenchmarkTestDto
        {
            TestName = "Tracking vs AsNoTracking",
            Description = "Compara el rendimiento de consultar todos los jugadores usando ToList() con change tracking activado versus AsNoTracking().ToList() que desactiva el seguimiento de cambios en EF Core",
            MethodA_Name = "ToList() con Tracking",
            MethodA_TimeMs = Math.Round(avgTracking, 3),
            MethodB_Name = "AsNoTracking().ToList()",
            MethodB_TimeMs = Math.Round(avgNoTracking, 3),
            ImprovementPercentage = Math.Round(improvement, 2),
            Winner = avgNoTracking < avgTracking ? "AsNoTracking" : "Tracking",
            RecordsProcessed = recordCount,
            Iterations = iterations
        };
    }

    private async Task<BenchmarkTestDto> BenchmarkIndividualVsBulkInsertAsync()
    {
        const int batchSize = 100;
        var sw = new Stopwatch();

        var individualScores = GenerateTestScores(batchSize);
        
        sw.Restart();
        foreach (var score in individualScores)
        {
            _context.Scores.Add(score);
            await _context.SaveChangesAsync();
        }
        sw.Stop();
        var individualTime = sw.Elapsed.TotalMilliseconds;

        await CleanupTestScoresAsync(individualScores);

        var bulkScores = GenerateTestScores(batchSize);

        sw.Restart();
        await _context.BulkInsertAsync(bulkScores);
        sw.Stop();
        var bulkTime = sw.Elapsed.TotalMilliseconds;

        await CleanupTestScoresAsync(bulkScores);

        var improvement = individualTime > 0 ? ((individualTime - bulkTime) / individualTime) * 100 : 0;

        return new BenchmarkTestDto
        {
            TestName = "Insert Individual vs BulkInsert",
            Description = "Compara la inserción de registros uno a uno llamando SaveChanges() por cada Score versus usar BulkInsert que ejecuta todos los inserts en una sola operación optimizada a nivel de base de datos",
            MethodA_Name = "Insert Individual (SaveChanges por registro)",
            MethodA_TimeMs = Math.Round(individualTime, 3),
            MethodB_Name = "BulkInsert (EFCore.BulkExtensions)",
            MethodB_TimeMs = Math.Round(bulkTime, 3),
            ImprovementPercentage = Math.Round(improvement, 2),
            Winner = bulkTime < individualTime ? "BulkInsert" : "Individual",
            RecordsProcessed = batchSize,
            Iterations = 1
        };
    }

    private async Task<BenchmarkTestDto> BenchmarkIndexedQueryAsync()
    {
        const int iterations = 5;
        var sw = new Stopwatch();

        var anyPlayer = await _context.Players.AsNoTracking().FirstOrDefaultAsync();
        var playerId = anyPlayer?.Id ?? Guid.NewGuid();

        var simpleQueryTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var scores = await _context.Scores
                .AsNoTracking()
                .Where(s => s.PlayerId == playerId)
                .ToListAsync();
            sw.Stop();
            simpleQueryTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var compositeQueryTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var scores = await _context.Scores
                .AsNoTracking()
                .Where(s => s.PlayerId == playerId)
                .OrderByDescending(s => s.AchievedAt)
                .ToListAsync();
            sw.Stop();
            compositeQueryTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avgSimple = simpleQueryTimes.Average();
        var avgComposite = compositeQueryTimes.Average();

        var scoreCount = await _context.Scores.CountAsync();

        return new BenchmarkTestDto
        {
            TestName = "Consulta Simple vs Índice Compuesto",
            Description = "Compara una consulta que filtra solo por PlayerId usando un índice simple versus una consulta que filtra por PlayerId y ordena por AchievedAt aprovechando el índice compuesto (PlayerId, AchievedAt) que cubre ambas operaciones",
            MethodA_Name = "Filtro por PlayerId (índice simple)",
            MethodA_TimeMs = Math.Round(avgSimple, 3),
            MethodB_Name = "Filtro PlayerId + orden AchievedAt (índice compuesto)",
            MethodB_TimeMs = Math.Round(avgComposite, 3),
            ImprovementPercentage = Math.Round(avgSimple > 0 ? ((avgSimple - avgComposite) / avgSimple) * 100 : 0, 2),
            Winner = avgComposite <= avgSimple ? "Índice Compuesto" : "Índice Simple",
            RecordsProcessed = scoreCount,
            Iterations = iterations
        };
    }


    private async Task<BenchmarkTestDto> BenchmarkPaginationVsFullLoadAsync()
    {
        const int iterations = 5;
        var sw = new Stopwatch();

        var fullLoadTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var allPlayers = await _context.Players
                .AsNoTracking()
                .OrderByDescending(p => p.TotalScore)
                .ToListAsync();
            sw.Stop();
            fullLoadTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var paginatedTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var pagedPlayers = await _context.Players
                .AsNoTracking()
                .OrderByDescending(p => p.TotalScore)
                .Skip(0)
                .Take(10)
                .ToListAsync();
            sw.Stop();
            paginatedTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avgFull = fullLoadTimes.Average();
        var avgPaginated = paginatedTimes.Average();
        var improvement = avgFull > 0 ? ((avgFull - avgPaginated) / avgFull) * 100 : 0;

        var totalRecords = await _context.Players.CountAsync();

        return new BenchmarkTestDto
        {
            TestName = "Carga Completa vs Paginación",
            Description = "Compara el rendimiento de cargar todos los jugadores de la base de datos a memoria versus usar paginación con Skip() y Take() para traer únicamente los primeros 10 registros, lo cual reduce la transferencia de datos y el uso de memoria",
            MethodA_Name = "Carga completa (ToList sin límite)",
            MethodA_TimeMs = Math.Round(avgFull, 3),
            MethodB_Name = "Paginación (Skip + Take 10)",
            MethodB_TimeMs = Math.Round(avgPaginated, 3),
            ImprovementPercentage = Math.Round(improvement, 2),
            Winner = avgPaginated < avgFull ? "Paginación" : "Carga Completa",
            RecordsProcessed = totalRecords,
            Iterations = iterations
        };
    }

    private async Task<BenchmarkTestDto> BenchmarkIndividualVsBulkUpdateAsync()
    {
        const int batchSize = 50;
        var sw = new Stopwatch();

        var testPlayers = GenerateTestPlayers(batchSize);
        await _context.BulkInsertAsync(testPlayers);

        sw.Restart();
        foreach (var player in testPlayers)
        {
            player.GamesPlayed += 1;
            _context.Players.Update(player);
            await _context.SaveChangesAsync();
        }
        sw.Stop();
        var individualTime = sw.Elapsed.TotalMilliseconds;

        foreach (var player in testPlayers)
        {
            player.Wins += 1;
        }

        sw.Restart();
        await _context.BulkUpdateAsync(testPlayers);
        sw.Stop();
        var bulkTime = sw.Elapsed.TotalMilliseconds;

        await _context.BulkDeleteAsync(testPlayers);

        var improvement = individualTime > 0 ? ((individualTime - bulkTime) / individualTime) * 100 : 0;

        return new BenchmarkTestDto
        {
            TestName = "Update Individual vs BulkUpdate",
            Description = "Compara la actualización de registros de manera individual llamando SaveChanges() por cada jugador modificado versus usar BulkUpdate que aplica todas las actualizaciones en una sola operación de base de datos optimizada",
            MethodA_Name = "Update Individual (SaveChanges por registro)",
            MethodA_TimeMs = Math.Round(individualTime, 3),
            MethodB_Name = "BulkUpdate (EFCore.BulkExtensions)",
            MethodB_TimeMs = Math.Round(bulkTime, 3),
            ImprovementPercentage = Math.Round(improvement, 2),
            Winner = bulkTime < individualTime ? "BulkUpdate" : "Individual",
            RecordsProcessed = batchSize,
            Iterations = 1
        };
    }

    private async Task<BenchmarkTestDto> BenchmarkRedisVsPostgresLeaderboardAsync()
    {
        const int iterations = 10;
        var sw = new Stopwatch();

        var testPlayers = GenerateTestPlayers(20);
        await _context.BulkInsertAsync(testPlayers);

        var playerId = testPlayers.First().Id;
        var sessionId = _context.GameSessions.AsNoTracking().Select(gs => gs.Id).FirstOrDefault();
        if (sessionId == Guid.Empty) sessionId = Guid.NewGuid();

        var testScores = new List<Score>();
        for (int i = 0; i < 50; i++)
        {
            testScores.Add(new Score
            {
                Id = Guid.NewGuid(),
                PlayerId = testPlayers[i % testPlayers.Count].Id,
                SessionId = sessionId,
                Points = Random.Shared.Next(100, 1000),
                AchievedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.BulkInsertAsync(testScores);

        await _redisCache.InvalidateLeaderboardCacheAsync();

        var postgresTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            await _redisCache.InvalidateLeaderboardCacheAsync();

            sw.Restart();
            var scores = await _context.Scores
                .AsNoTracking()
                .Include(s => s.Player)
                .OrderByDescending(s => s.Points)
                .Take(10)
                .ToListAsync();
            sw.Stop();
            postgresTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        var leaderboardData = await _context.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .OrderByDescending(s => s.Points)
            .Take(10)
            .Select(s => new { PlayerName = s.Player!.Username, s.Points, s.AchievedAt })
            .ToListAsync();
        var json = JsonSerializer.Serialize(leaderboardData);
        await _redisCache.CacheLeaderboardAsync(json, 10);

        var redisTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var cached = await _redisCache.GetCachedLeaderboardAsync(10);
            sw.Stop();
            redisTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        await _context.BulkDeleteAsync(testScores);
        await _context.BulkDeleteAsync(testPlayers);
        await _redisCache.InvalidateLeaderboardCacheAsync();

        var avgPostgres = postgresTimes.Average();
        var avgRedis = redisTimes.Average();
        var improvement = avgPostgres > 0 ? ((avgPostgres - avgRedis) / avgPostgres) * 100 : 0;

        return new BenchmarkTestDto
        {
            TestName = "Leaderboard: PostgreSQL vs Redis Cache",
            Description = "Compara el tiempo de obtener el top 10 del leaderboard directamente desde PostgreSQL con JOIN e índices versus leer el resultado pre-cacheado en Redis como un string JSON",
            MethodA_Name = "PostgreSQL (Query con JOIN + ORDER BY)",
            MethodA_TimeMs = Math.Round(avgPostgres, 3),
            MethodB_Name = "Redis Cache (StringGet)",
            MethodB_TimeMs = Math.Round(avgRedis, 3),
            ImprovementPercentage = Math.Round(improvement, 2),
            Winner = avgRedis < avgPostgres ? "Redis Cache" : "PostgreSQL",
            RecordsProcessed = 10,
            Iterations = iterations
        };
    }

    private async Task<BenchmarkTestDto> BenchmarkRedisVsPostgresSessionAsync()
    {
        const int iterations = 10;
        var sw = new Stopwatch();

        var anyPlayer = await _context.Players.AsNoTracking().FirstOrDefaultAsync();
        if (anyPlayer == null)
        {
            return new BenchmarkTestDto
            {
                TestName = "Sesión: PostgreSQL vs Redis Cache",
                Description = "No hay jugadores para ejecutar este test",
                MethodA_Name = "PostgreSQL",
                MethodA_TimeMs = 0,
                MethodB_Name = "Redis",
                MethodB_TimeMs = 0,
                ImprovementPercentage = 0,
                Winner = "N/A",
                RecordsProcessed = 0,
                Iterations = 0
            };
        }

        var firebaseUid = anyPlayer.FirebaseUid;

        var postgresTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.FirebaseUid == firebaseUid)
                .FirstOrDefaultAsync();
            sw.Stop();
            postgresTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        await _redisCache.CacheSessionAsync(firebaseUid, new Dictionary<string, string>
        {
            ["PlayerId"] = anyPlayer.Id.ToString(),
            ["Username"] = anyPlayer.Username,
            ["Email"] = anyPlayer.Email
        });

        var redisTimes = new List<double>();
        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var session = await _redisCache.GetCachedSessionAsync(firebaseUid);
            sw.Stop();
            redisTimes.Add(sw.Elapsed.TotalMilliseconds);
        }

        await _redisCache.InvalidateSessionAsync(firebaseUid);

        var avgPostgres = postgresTimes.Average();
        var avgRedis = redisTimes.Average();
        var improvement = avgPostgres > 0 ? ((avgPostgres - avgRedis) / avgPostgres) * 100 : 0;

        return new BenchmarkTestDto
        {
            TestName = "Sesión: PostgreSQL vs Redis Cache",
            Description = "Compara el tiempo de buscar un jugador por su FirebaseUid en PostgreSQL versus obtener los datos de sesión cacheados en Redis usando un Hash",
            MethodA_Name = "PostgreSQL (WHERE FirebaseUid)",
            MethodA_TimeMs = Math.Round(avgPostgres, 3),
            MethodB_Name = "Redis Cache (HashGetAll)",
            MethodB_TimeMs = Math.Round(avgRedis, 3),
            ImprovementPercentage = Math.Round(improvement, 2),
            Winner = avgRedis < avgPostgres ? "Redis Cache" : "PostgreSQL",
            RecordsProcessed = 1,
            Iterations = iterations
        };
    }

    private List<Score> GenerateTestScores(int count)
    {
        var scores = new List<Score>();
        var playerId = _context.Players.AsNoTracking().Select(p => p.Id).FirstOrDefault();
        var sessionId = _context.GameSessions.AsNoTracking().Select(gs => gs.Id).FirstOrDefault();

        if (playerId == Guid.Empty) playerId = Guid.NewGuid();
        if (sessionId == Guid.Empty) sessionId = Guid.NewGuid();

        for (int i = 0; i < count; i++)
        {
            scores.Add(new Score
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                SessionId = sessionId,
                Points = Random.Shared.Next(10, 500),
                AchievedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        return scores;
    }

    private List<Player> GenerateTestPlayers(int count)
    {
        var players = new List<Player>();
        for (int i = 0; i < count; i++)
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            players.Add(new Player
            {
                Id = Guid.NewGuid(),
                FirebaseUid = $"benchmark_test_{uniqueId}",
                Username = $"bench_{uniqueId}",
                Email = $"bench_{uniqueId}@test.com",
                GamesPlayed = 0,
                Wins = 0,
                TotalScore = Random.Shared.Next(0, 1000),
                CreatedAt = DateTime.UtcNow
            });
        }
        return players;
    }

    private async Task CleanupTestScoresAsync(IList<Score> scores)
    {
        var ids = scores.Select(s => s.Id).ToList();
        var toDelete = await _context.Scores
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        if (toDelete.Any())
        {
            _context.Scores.RemoveRange(toDelete);
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateSummary(List<BenchmarkTestDto> tests)
    {
        var summaryLines = new List<string>
        {
            "=== RESUMEN DEL BENCHMARKING ===",
            ""
        };

        foreach (var test in tests)
        {
            summaryLines.Add($"• {test.TestName}: {test.Winner} gana con {Math.Abs(test.ImprovementPercentage):F1}% de mejora");
            summaryLines.Add($"  [{test.MethodA_Name}: {test.MethodA_TimeMs}ms] vs [{test.MethodB_Name}: {test.MethodB_TimeMs}ms]");
            summaryLines.Add("");
        }

        return string.Join("\n", summaryLines);
    }
}
