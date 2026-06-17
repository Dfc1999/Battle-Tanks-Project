using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Infrastructure.Data;

namespace BattleTanks_Backend.Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly BattleTanksDbContext _context;

    public PlayerRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Player>> GetAllAsync()
    {
        return await _context.Players
            .AsNoTracking()
            .OrderByDescending(p => p.TotalScore)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Player> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize)
    {
        var query = _context.Players
            .AsNoTracking()
            .OrderByDescending(p => p.TotalScore);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Player?> GetByIdAsync(Guid id)
    {
        return await _context.Players
            .Include(p => p.Scores)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Player?> GetByUsernameAsync(string username)
    {
        return await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Username.ToLower() == username.ToLower());
    }

    public async Task<Player?> GetByEmailAsync(string email)
    {
        return await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower());
    }

    public async Task<Player?> GetByFirebaseUidAsync(string firebaseUid)
    {
        return await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.FirebaseUid == firebaseUid);
    }

    public async Task<Player> CreateAsync(Player player)
    {
        player.Id = Guid.NewGuid();
        player.CreatedAt = DateTime.UtcNow;

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        return player;
    }

    public async Task<Player> UpdateAsync(Player player)
    {
        _context.Players.Update(player);
        await _context.SaveChangesAsync();

        return player;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player == null) return false;

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _context.Players
            .AsNoTracking()
            .AnyAsync(p => p.Username.ToLower() == username.ToLower());
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Players
            .AsNoTracking()
            .AnyAsync(p => p.Email.ToLower() == email.ToLower());
    }

    public async Task BulkInsertPlayersAsync(IList<Player> players)
    {
        await _context.BulkInsertAsync(players);
    }

    public async Task BulkUpdatePlayersAsync(IList<Player> players)
    {
        await _context.BulkUpdateAsync(players);
    }

    public async Task<List<Player>> GetAllWithTrackingAsync()
    {
        return await _context.Players
            .OrderByDescending(p => p.TotalScore)
            .ToListAsync();
    }

    public async Task<List<Player>> GetAllWithoutTrackingAsync()
    {
        return await _context.Players
            .AsNoTracking()
            .OrderByDescending(p => p.TotalScore)
            .ToListAsync();
    }
}
