using BattleTanks_Backend.Application.DTOs.Player;

namespace BattleTanks_Backend.Application.Interfaces;

public interface IPlayerService
{
    Task<IEnumerable<PlayerDto>> GetAllPlayersAsync();
    Task<PlayerDto?> GetPlayerByIdAsync(Guid id);
    Task<PlayerDto?> GetPlayerByUsernameAsync(string username);
    Task<PlayerDto> CreatePlayerAsync(CreatePlayerDto dto);
    Task<PlayerDto?> UpdatePlayerAsync(Guid id, UpdatePlayerDto dto);
    Task<bool> DeletePlayerAsync(Guid id);
}
