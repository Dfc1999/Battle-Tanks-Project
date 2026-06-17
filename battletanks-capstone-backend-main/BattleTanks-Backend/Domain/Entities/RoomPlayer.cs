using System.ComponentModel.DataAnnotations;

namespace BattleTanks_Backend.Domain.Entities;

public class RoomPlayer
{
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public Guid PlayerId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public GameSession Session { get; set; } = null!;

    public Player Player { get; set; } = null!;
}
