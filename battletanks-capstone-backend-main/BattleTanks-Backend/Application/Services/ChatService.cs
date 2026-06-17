using BattleTanks_Backend.Application.DTOs.Chat;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Interfaces;

namespace BattleTanks_Backend.Application.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IPlayerRepository _playerRepository;

    public ChatService(IChatRepository chatRepository, IPlayerRepository playerRepository)
    {
        _chatRepository = chatRepository;
        _playerRepository = playerRepository;
    }

    public async Task<ChatHistoryResponse> GetChatHistoryAsync(Guid roomId, int limit = 50)
    {
        var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId, limit);
        var totalCount = await _chatRepository.GetMessageCountByRoomIdAsync(roomId);

        return new ChatHistoryResponse
        {
            RoomId = roomId,
            TotalMessages = totalCount,
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                RoomId = m.RoomId,
                PlayerId = m.PlayerId,
                PlayerName = m.PlayerName,
                Message = m.Message,
                Timestamp = m.Timestamp
            }).ToList()
        };
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid playerId, CreateChatMessageDto dto)
    {
        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null)
        {
            throw new InvalidOperationException("Player not found");
        }

        var chatMessage = new ChatMessage
        {
            RoomId = dto.RoomId,
            PlayerId = playerId,
            PlayerName = player.Username,
            Message = dto.Message
        };

        var created = await _chatRepository.CreateMessageAsync(chatMessage);

        return new ChatMessageDto
        {
            Id = created.Id,
            RoomId = created.RoomId,
            PlayerId = created.PlayerId,
            PlayerName = created.PlayerName,
            Message = created.Message,
            Timestamp = created.Timestamp
        };
    }
}
