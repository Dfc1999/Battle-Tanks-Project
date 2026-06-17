namespace BattleTanks_Backend.Application.DTOs.Score;

public class ScoreDto
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public int Points { get; set; }
    public DateTime AchievedAt { get; set; }
}

public class CreateScoreDto
{
    public Guid PlayerId { get; set; }
    public Guid SessionId { get; set; }
    public int Points { get; set; }
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Points { get; set; }
    public DateTime AchievedAt { get; set; }
}
