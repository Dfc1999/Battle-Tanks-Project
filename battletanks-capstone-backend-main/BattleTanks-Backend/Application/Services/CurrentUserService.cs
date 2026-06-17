using System.Security.Claims;

namespace BattleTanks_Backend.Application.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    string? FirebaseUid { get; }
    Guid? PlayerId { get; }
    string? Username { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? FirebaseUid => _httpContextAccessor.HttpContext?.User?.FindFirst("FirebaseUid")?.Value
        ?? _httpContextAccessor.HttpContext?.Items["FirebaseUid"]?.ToString();

    public Guid? PlayerId
    {
        get
        {
            var playerIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("PlayerId")?.Value;
            return Guid.TryParse(playerIdClaim, out var guid) ? guid : null;
        }
    }

    public string? Username => _httpContextAccessor.HttpContext?.User?.FindFirst("Username")?.Value;
}
