using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Infrastructure.Data;

namespace BattleTanks_Backend.Infrastructure.Repositories;

public class ScoreRepository : IScoreRepository
{
    private readonly BattleTanksDbContext _context;

    public ScoreRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Score>> GetAllAsync()
    {
        return await _context.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Session)
            .OrderByDescending(s => s.Points)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Score> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize)
    {
        var query = _context.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Session)
            .OrderByDescending(s => s.Points);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Score?> GetByIdAsync(Guid id)
    {
        return await _context.Scores
            .Include(s => s.Player)
            .Include(s => s.Session)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Score>> GetByPlayerIdAsync(Guid playerId)
    {
        return await _context.Scores
            .AsNoTracking()
            .Include(s => s.Session)
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.AchievedAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Score> Items, int TotalCount)> GetByPlayerIdPaginatedAsync(Guid playerId, int page, int pageSize)
    {
        var query = _context.Scores
            .AsNoTracking()
            .Include(s => s.Session)
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.AchievedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Score>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.SessionId == sessionId)
            .OrderByDescending(s => s.Points)
            .ToListAsync();
    }

    public async Task<IEnumerable<Score>> GetLeaderboardAsync(int top = 10)
    {
        var topPlayers = await _context.Players
            .AsNoTracking()
            .OrderByDescending(p => p.TotalScore)
            .Take(top)
            .ToListAsync();

        return topPlayers.Select(p => new Score
        {
            PlayerId = p.Id,
            Player = p,
            Points = p.TotalScore,
            AchievedAt = p.LastLogin ?? p.CreatedAt
        });
    }

    public async Task<Score> CreateAsync(Score score)
    {
        score.Id = Guid.NewGuid();
        score.AchievedAt = DateTime.UtcNow;

        _context.Scores.Add(score);
        await _context.SaveChangesAsync();

        var player = await _context.Players.FindAsync(score.PlayerId);
        if (player != null)
        {
            player.TotalScore += score.Points;
            player.GamesPlayed++;
            await _context.SaveChangesAsync();
        }

        return score;
    }

    public async Task<Score> UpdateAsync(Score score)
    {
        _context.Scores.Update(score);
        await _context.SaveChangesAsync();

        return score;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var score = await _context.Scores.FindAsync(id);
        if (score == null) return false;

        _context.Scores.Remove(score);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task BulkInsertScoresAsync(IList<Score> scores)
    {
        await _context.BulkInsertAsync(scores);
    }
}
