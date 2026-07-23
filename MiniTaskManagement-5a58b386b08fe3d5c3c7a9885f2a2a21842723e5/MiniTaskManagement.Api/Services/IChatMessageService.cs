using MiniTaskManagement.Api.DTOs;

namespace MiniTaskManagement.Api.Services;

public interface IChatMessageService
{
    Task<List<ChatMessageDto>> GetMessages(Guid currentUserId, Guid roomId, int page, int pageSize);

    Task<ChatMessageDto> SendMessage(Guid currentUserId, Guid roomId, SendMessageRequest request);

    Task<ChatMessageDto> UpdateMessage(Guid currentUserId, Guid messageId, SendMessageRequest request);

    Task<ChatMessageDto> DeleteMessage(Guid currentUserId, Guid messageId);

    Task MarkAsRead(Guid currentUserId, Guid roomId);
}
