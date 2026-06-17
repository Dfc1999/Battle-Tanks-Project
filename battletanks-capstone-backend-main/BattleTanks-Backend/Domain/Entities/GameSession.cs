using System.ComponentModel.DataAnnotations;
using BattleTanks_Backend.Domain.Enums;

namespace BattleTanks_Backend.Domain.Entities;

public class GameSession
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SelectedMap { get; set; } = "default";

    [MaxLength(10)]
    public string Region { get; set; } = "LATAM";

    public GameStatus Status { get; set; } = GameStatus.Waiting;

    [Range(2, 4)]
    public int MaxPlayers { get; set; } = 4;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public ICollection<Score> Scores { get; set; } = new List<Score>();
}
