namespace BattleTanks_Backend.Application.DTOs.Chat;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class CreateChatMessageDto
{
    public Guid RoomId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatHistoryResponse
{
    public Guid RoomId { get; set; }
    public int TotalMessages { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}
