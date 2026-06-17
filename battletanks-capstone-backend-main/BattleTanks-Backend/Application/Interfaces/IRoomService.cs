using BattleTanks_Backend.Application.DTOs.Room;

namespace BattleTanks_Backend.Application.Interfaces;

public interface IRoomService
{
    Task<IEnumerable<RoomDto>> GetAvailableRoomsAsync();
    Task<RoomDto?> GetRoomByIdAsync(Guid id);
    Task<RoomDto> CreateRoomAsync(CreateRoomDto dto, Guid creatorPlayerId);
    Task<JoinRoomResponseDto> JoinRoomAsync(Guid roomId, Guid playerId);
    Task<JoinRoomResponseDto> LeaveRoomAsync(Guid roomId, Guid playerId);
}
