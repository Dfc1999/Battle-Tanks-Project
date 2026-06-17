namespace BattleTanks_Backend.Application.DTOs.Room;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SelectedMap { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RoomPlayerDto> Players { get; set; } = new();
}

public class RoomPlayerDto
{
    public Guid PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class CreateRoomDto
{
    public string Name { get; set; } = string.Empty;
    public string SelectedMap { get; set; } = "default";
    public int MaxPlayers { get; set; } = 4;
}

public class JoinRoomResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RoomDto? Room { get; set; }
}
