using BattleTanks_Backend.Domain.Entities;

namespace BattleTanks_Backend.Domain.Interfaces;

public interface IPlayerRepository
{
    Task<IEnumerable<Player>> GetAllAsync();

    Task<(IEnumerable<Player> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize);

    Task<Player?> GetByIdAsync(Guid id);

    Task<Player?> GetByUsernameAsync(string username);

    Task<Player?> GetByEmailAsync(string email);

    Task<Player?> GetByFirebaseUidAsync(string firebaseUid);

    Task<Player> CreateAsync(Player player);

    Task<Player> UpdateAsync(Player player);

    Task<bool> DeleteAsync(Guid id);

    Task<bool> ExistsByUsernameAsync(string username);

    Task<bool> ExistsByEmailAsync(string email);

    Task BulkInsertPlayersAsync(IList<Player> players);

    Task BulkUpdatePlayersAsync(IList<Player> players);

    Task<List<Player>> GetAllWithTrackingAsync();
    Task<List<Player>> GetAllWithoutTrackingAsync();
}
