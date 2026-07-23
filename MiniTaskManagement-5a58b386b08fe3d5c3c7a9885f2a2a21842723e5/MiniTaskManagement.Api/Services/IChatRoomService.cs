using MiniTaskManagement.Api.DTOs;

namespace MiniTaskManagement.Api.Services;

public interface IChatRoomService
{
    Task<List<ChatUserDto>> GetChatUsers(Guid currentUserId);

    Task<List<ChatRoomDto>> GetRooms(Guid currentUserId);

    Task<ChatRoomDto> GetOrCreatePrivateRoom(Guid currentUserId, Guid otherUserId);

    Task<ChatRoomDto> GetOrCreateSupportRoom(Guid currentUserId, Guid? adminId);

    Task<ChatRoomDto> CreateGroupRoom(Guid currentUserId, CreateGroupChatRequest request);

    Task<ChatRoomDto> GetOrCreateProjectRoom(Guid currentUserId, Guid projectId);

    Task<ChatRoomDto> GetOrCreateTaskRoom(Guid currentUserId, Guid taskId);

    Task<ChatRoomDto> AddMember(Guid currentUserId, Guid roomId, AddMemberRequest request);

    Task<ChatRoomDto> RemoveMember(Guid currentUserId, Guid roomId, Guid userId);

    Task<ChatRoomDto> UpdateRoom(Guid currentUserId, Guid roomId, UpdateRoomRequest request);

    Task DeleteRoom(Guid currentUserId, Guid roomId);

    Task<bool> IsActiveMember(Guid userId, Guid roomId);

    Task<List<Guid>> GetActiveMemberIds(Guid roomId);
}
