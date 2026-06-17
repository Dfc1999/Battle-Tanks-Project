using Microsoft.EntityFrameworkCore;
using BattleTanks_Backend.Domain.Interfaces;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Infrastructure.Data;

namespace BattleTanks_Backend.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly BattleTanksDbContext _context;

    public ChatRepository(BattleTanksDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessage>> GetMessagesByRoomIdAsync(Guid roomId, int limit = 50)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<ChatMessage> CreateMessageAsync(ChatMessage message)
    {
        message.Id = Guid.NewGuid();
        message.Timestamp = DateTime.UtcNow;

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        return message;
    }

    public async Task<int> GetMessageCountByRoomIdAsync(Guid roomId)
    {
        return await _context.ChatMessages
            .Where(m => m.RoomId == roomId)
            .CountAsync();
    }
}
