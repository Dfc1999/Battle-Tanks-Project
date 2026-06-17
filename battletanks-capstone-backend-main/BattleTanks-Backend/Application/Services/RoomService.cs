using BattleTanks_Backend.Application.DTOs.Room;
using BattleTanks_Backend.Application.Interfaces;
using BattleTanks_Backend.Domain.Entities;
using BattleTanks_Backend.Domain.Enums;
using BattleTanks_Backend.Domain.Interfaces;

namespace BattleTanks_Backend.Application.Services;

public class RoomService : IRoomService
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IPlayerRepository _playerRepository;

    public RoomService(
        IGameSessionRepository sessionRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IPlayerRepository playerRepository)
    {
        _sessionRepository = sessionRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _playerRepository = playerRepository;
    }

    public async Task<IEnumerable<RoomDto>> GetAvailableRoomsAsync()
    {
        var sessions = await _sessionRepository.GetByStatusAsync(GameStatus.Waiting);
        var roomDtos = new List<RoomDto>();

        foreach (var session in sessions)
        {
            var players = await _roomPlayerRepository.GetBySessionIdAsync(session.Id);
            var playerCount = players.Count();

            if (playerCount < session.MaxPlayers)
            {
                roomDtos.Add(await MapToRoomDto(session));
            }
        }

        return roomDtos;
    }

    public async Task<RoomDto?> GetRoomByIdAsync(Guid id)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null) return null;

        return await MapToRoomDto(session);
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomDto dto, Guid creatorPlayerId)
    {
        var session = new GameSession
        {
            Name = dto.Name,
            SelectedMap = dto.SelectedMap,
            MaxPlayers = Math.Clamp(dto.MaxPlayers, 2, 4),
            Status = GameStatus.Waiting,
            CreatedAt = DateTime.UtcNow
        };

        var createdSession = await _sessionRepository.CreateAsync(session);

        var roomPlayer = new RoomPlayer
        {
            SessionId = createdSession.Id,
            PlayerId = creatorPlayerId,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _roomPlayerRepository.AddPlayerToRoomAsync(roomPlayer);

        return await MapToRoomDto(createdSession);
    }

    public async Task<JoinRoomResponseDto> JoinRoomAsync(Guid roomId, Guid playerId)
    {
        var session = await _sessionRepository.GetByIdAsync(roomId);
        if (session == null)
        {
            return new JoinRoomResponseDto
            {
                Success = false,
                Message = "Sala no encontrada"
            };
        }

        if (session.Status != GameStatus.Waiting)
        {
            return new JoinRoomResponseDto
            {
                Success = false,
                Message = "La sala ya no está disponible"
            };
        }

        var existingPlayer = await _roomPlayerRepository.GetByPlayerAndSessionAsync(playerId, roomId);
        if (existingPlayer != null)
        {
            return new JoinRoomResponseDto
            {
                Success = false,
                Message = "Ya estás en esta sala"
            };
        }

        if (await _roomPlayerRepository.IsPlayerInAnyRoomAsync(playerId))
        {
            return new JoinRoomResponseDto
            {
                Success = false,
                Message = "Ya estás en otra sala. Debes salir primero."
            };
        }

        var currentCount = await _roomPlayerRepository.GetActivePlayerCountAsync(roomId);
        if (currentCount >= session.MaxPlayers)
        {
            return new JoinRoomResponseDto
            {
                Success = false,
                Message = $"La sala está llena (máximo {session.MaxPlayers} jugadores)"
            };
        }

        var roomPlayer = new RoomPlayer
        {
            SessionId = roomId,
            PlayerId = playerId,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _roomPlayerRepository.AddPlayerToRoomAsync(roomPlayer);

        return new JoinRoomResponseDto
        {
            Success = true,
            Message = "Te has unido a la sala exitosamente",
            Room = await MapToRoomDto(session)
        };
    }

    public async Task<JoinRoomResponseDto> LeaveRoomAsync(Guid roomId, Guid playerId)
    {
        var session = await _sessionRepository.GetByIdAsync(roomId);
        if (session == null)
        {
            return new JoinRoomResponseDto
            {
                Success = false,
                Message = "Sala no encontrada"
            };
        }

        var removed = await _roomPlayerRepository.RemovePlayerFromRoomAsync(playerId, roomId);
        if (!removed)
        {
            return new JoinRoomResponseDto
            {
                Success = false,
                Message = "No estás en esta sala"
            };
        }

        return new JoinRoomResponseDto
        {
            Success = true,
            Message = "Has salido de la sala",
            Room = await MapToRoomDto(session)
        };
    }

    private async Task<RoomDto> MapToRoomDto(GameSession session)
    {
        var players = await _roomPlayerRepository.GetBySessionIdAsync(session.Id);

        return new RoomDto
        {
            Id = session.Id,
            Name = session.Name,
            SelectedMap = session.SelectedMap,
            Status = session.Status.ToString(),
            MaxPlayers = session.MaxPlayers,
            CurrentPlayers = players.Count(),
            CreatedAt = session.CreatedAt,
            Players = players.Select(rp => new RoomPlayerDto
            {
                PlayerId = rp.PlayerId,
                Username = rp.Player?.Username ?? "Unknown",
                JoinedAt = rp.JoinedAt
            }).ToList()
        };
    }
}
