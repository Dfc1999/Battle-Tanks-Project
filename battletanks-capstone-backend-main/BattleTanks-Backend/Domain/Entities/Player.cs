using System.ComponentModel.DataAnnotations;

namespace BattleTanks_Backend.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }

    [MaxLength(128)]
    public string FirebaseUid { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    public int GamesPlayed { get; set; } = 0;

    public int Wins { get; set; } = 0;

    public int TotalScore { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }

    public ICollection<Score> Scores { get; set; } = new List<Score>();
}
