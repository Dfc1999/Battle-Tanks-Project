using System.ComponentModel.DataAnnotations;

namespace BattleTanks_Backend.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }

    [Required]
    public Guid RoomId { get; set; }

    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    [MaxLength(50)]
    public string PlayerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public GameSession Room { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
