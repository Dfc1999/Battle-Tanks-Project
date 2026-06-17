using BattleTanks_Backend.Domain.Enums;

namespace BattleTanks_Backend.Application.DTOs.GameSession;

public class GameSessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SelectedMap { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class CreateSessionDto
{
    public string Name { get; set; } = string.Empty;
    public string SelectedMap { get; set; } = "default";
    public int MaxPlayers { get; set; } = 4;
}

public class UpdateSessionStatusDto
{
    public GameStatus Status { get; set; }
}
