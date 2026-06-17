using BattleTanks_Backend.Application.DTOs.Auth;

namespace BattleTanks_Backend.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<PlayerAuthDto?> GetCurrentPlayerAsync(string firebaseUid);
    Task<PlayerAuthDto?> GetOrCreatePlayerFromFirebaseAsync(string firebaseUid, string email, string username);
    Task<string?> VerifyTokenAsync(string token);
}
