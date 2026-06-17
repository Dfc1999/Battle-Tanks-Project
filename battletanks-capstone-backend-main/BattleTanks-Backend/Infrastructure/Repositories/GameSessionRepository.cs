using Microsoft.EntityFrameworkCore;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Enums;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Infrastructure.Data;

namespace BattleTanks_Backend.Infrastructure.Repositories;

public class GameSessionRepository : IGameSessionRepository
{
    private readonly BattleTanksDbContext _context;

    public GameSessionRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GameSession>> GetAllAsync()
    {
        return await _context.GameSessions
            .AsNoTracking()
            .Include(gs => gs.Scores)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<GameSession> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize)
    {
        var query = _context.GameSessions
            .AsNoTracking()
            .Include(gs => gs.Scores)
            .OrderByDescending(gs => gs.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<GameSession?> GetByIdAsync(Guid id)
    {
        return await _context.GameSessions
            .Include(gs => gs.Scores)
                .ThenInclude(s => s.Player)
            .FirstOrDefaultAsync(gs => gs.Id == id);
    }

    public async Task<IEnumerable<GameSession>> GetByStatusAsync(GameStatus status)
    {
        return await _context.GameSessions
            .AsNoTracking()
            .Where(gs => gs.Status == status)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<GameSession>> GetActiveSessionsAsync()
    {
        return await _context.GameSessions
            .AsNoTracking()
            .Where(gs => gs.Status == GameStatus.Waiting || gs.Status == GameStatus.InProgress)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync();
    }

    public async Task<GameSession> CreateAsync(GameSession session)
    {
        session.Id = Guid.NewGuid();
        session.CreatedAt = DateTime.UtcNow;
        session.Status = GameStatus.Waiting;

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<GameSession> UpdateAsync(GameSession session)
    {
        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var session = await _context.GameSessions.FindAsync(id);
        if (session == null) return false;

        _context.GameSessions.Remove(session);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<GameSession?> UpdateStatusAsync(Guid id, GameStatus newStatus)
    {
        var session = await _context.GameSessions.FindAsync(id);
        if (session == null) return null;

        session.Status = newStatus;

        if (newStatus == GameStatus.InProgress)
        {
            session.StartedAt = DateTime.UtcNow;
        }
        else if (newStatus == GameStatus.Finished)
        {
            session.EndedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return session;
    }
}
