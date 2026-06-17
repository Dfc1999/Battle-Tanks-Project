namespace BattleTanksAPI.Hubs;

public class PlayerPosition
{
    public string PlayerId { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; }
}

public class HubChatMessage
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class HubPlayer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public string TankType { get; set; } = "e100";
}

public class GameMessage
{
    public string Type { get; set; } = string.Empty;
    public object? Data { get; set; }
}
