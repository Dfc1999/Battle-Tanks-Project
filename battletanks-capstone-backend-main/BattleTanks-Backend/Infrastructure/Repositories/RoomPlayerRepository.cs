using Microsoft.EntityFrameworkCore;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Infrastructure.Data;

namespace BattleTanks_Backend.Infrastructure.Repositories;

public class RoomPlayerRepository : IRoomPlayerRepository
{
    private readonly BattleTanksDbContext _context;

    public RoomPlayerRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoomPlayer>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.RoomPlayers
            .AsNoTracking()
            .Include(rp => rp.Player)
            .Where(rp => rp.SessionId == sessionId && rp.IsActive)
            .OrderBy(rp => rp.JoinedAt)
            .ToListAsync();
    }

    public async Task<RoomPlayer?> GetByPlayerAndSessionAsync(Guid playerId, Guid sessionId)
    {
        return await _context.RoomPlayers
            .FirstOrDefaultAsync(rp => rp.PlayerId == playerId && rp.SessionId == sessionId && rp.IsActive);
    }

    public async Task<int> GetActivePlayerCountAsync(Guid sessionId)
    {
        return await _context.RoomPlayers
            .CountAsync(rp => rp.SessionId == sessionId && rp.IsActive);
    }

    public async Task<RoomPlayer> AddPlayerToRoomAsync(RoomPlayer roomPlayer)
    {
        var existing = await _context.RoomPlayers
            .FirstOrDefaultAsync(rp => rp.PlayerId == roomPlayer.PlayerId && rp.SessionId == roomPlayer.SessionId);

        if (existing != null)
        {
            existing.IsActive = true;
            existing.JoinedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        roomPlayer.Id = Guid.NewGuid();
        roomPlayer.JoinedAt = DateTime.UtcNow;
        roomPlayer.IsActive = true;

        _context.RoomPlayers.Add(roomPlayer);
        await _context.SaveChangesAsync();

        return roomPlayer;
    }

    public async Task<bool> RemovePlayerFromRoomAsync(Guid playerId, Guid sessionId)
    {
        var roomPlayer = await _context.RoomPlayers
            .FirstOrDefaultAsync(rp => rp.PlayerId == playerId && rp.SessionId == sessionId && rp.IsActive);

        if (roomPlayer == null) return false;

        roomPlayer.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsPlayerInAnyRoomAsync(Guid playerId)
    {
        return await _context.RoomPlayers
            .AnyAsync(rp => rp.PlayerId == playerId && rp.IsActive);
    }
}
