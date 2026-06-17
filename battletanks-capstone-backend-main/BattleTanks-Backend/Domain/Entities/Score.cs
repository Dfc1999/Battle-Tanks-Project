using System.ComponentModel.DataAnnotations;

namespace BattleTanks_Backend.Domain.Entities;

public class Score
{
    public Guid Id { get; set; }

    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Range(0, int.MaxValue)]
    public int Points { get; set; }

    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;

    public Player Player { get; set; } = null!;

    public GameSession Session { get; set; } = null!;
}
