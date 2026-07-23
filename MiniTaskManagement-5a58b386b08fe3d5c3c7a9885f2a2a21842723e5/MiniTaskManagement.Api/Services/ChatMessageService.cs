using Microsoft.EntityFrameworkCore;
using MiniTaskManagement.Api.Data;
using MiniTaskManagement.Api.DTOs;
using MiniTaskManagement.Api.Entities;

namespace MiniTaskManagement.Api.Services;

public class ChatMessageService : IChatMessageService
{
    private readonly AppDbContext _dbContext;
    private readonly IChatRoomService _chatRoomService;

    public ChatMessageService(
        AppDbContext dbContext,
        IChatRoomService chatRoomService)
    {
        _dbContext = dbContext;
        _chatRoomService = chatRoomService;
    }

    public async Task<List<ChatMessageDto>> GetMessages(
        Guid currentUserId,
        Guid roomId,
        int page,
        int pageSize)
    {
        await EnsureMember(currentUserId, roomId);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var messages = await _dbContext.ChatMessages
            .Where(message => message.ChatRoomId == roomId)
            .Include(message => message.Sender)
            .Include(message => message.Reads)
            .ThenInclude(read => read.User)
            .Include(message => message.ReplyToMessage)
            .ThenInclude(reply => reply!.Sender)
            .OrderByDescending(message => message.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return messages
            .OrderBy(message => message.CreatedAt)
            .Select(MapMessage)
            .ToList();
    }

    public async Task<ChatMessageDto> SendMessage(
        Guid currentUserId,
        Guid roomId,
        SendMessageRequest request)
    {
        await EnsureMember(currentUserId, roomId);

        if (string.IsNullOrWhiteSpace(request.Content) &&
            string.IsNullOrWhiteSpace(request.AttachmentUrl))
        {
            throw new ArgumentException("Message content or attachment is required");
        }

        if (request.ReplyToMessageId.HasValue)
        {
            var replyExists = await _dbContext.ChatMessages
                .AnyAsync(message =>
                    message.Id == request.ReplyToMessageId.Value &&
                    message.ChatRoomId == roomId &&
                    !message.IsDeleted);

            if (!replyExists)
                throw new ChatNotFoundException("Reply message not found");
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatRoomId = roomId,
            SenderId = currentUserId,
            Content = request.Content.Trim(),
            MessageType = request.MessageType,
            AttachmentUrl = request.AttachmentUrl,
            ReplyToMessageId = request.ReplyToMessageId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ChatMessages.Add(message);

        _dbContext.ChatMessageReads.Add(new ChatMessageRead
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            UserId = currentUserId,
            ReadAt = DateTime.UtcNow
        });

        var room = await _dbContext.ChatRooms
            .FirstOrDefaultAsync(x => x.Id == roomId);

        if (room != null)
            room.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetMessageDto(message.Id);
    }

    public async Task<ChatMessageDto> UpdateMessage(
        Guid currentUserId,
        Guid messageId,
        SendMessageRequest request)
    {
        var message = await _dbContext.ChatMessages
            .Include(x => x.Sender)
            .FirstOrDefaultAsync(x => x.Id == messageId);

        if (message == null)
            throw new ChatNotFoundException("Message not found");

        await EnsureMember(currentUserId, message.ChatRoomId);

        if (message.SenderId != currentUserId)
            throw new ChatForbiddenException("Only sender can edit this message");

        if (message.IsDeleted)
            throw new ChatForbiddenException("Cannot edit a deleted message");

        if (string.IsNullOrWhiteSpace(request.Content) &&
            string.IsNullOrWhiteSpace(request.AttachmentUrl))
        {
            throw new ArgumentException("Message content or attachment is required");
        }

        message.Content = request.Content.Trim();
        message.MessageType = request.MessageType;
        message.AttachmentUrl = request.AttachmentUrl;
        message.EditedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetMessageDto(message.Id);
    }

    public async Task<ChatMessageDto> DeleteMessage(
        Guid currentUserId,
        Guid messageId)
    {
        var message = await _dbContext.ChatMessages
            .Include(x => x.Sender)
            .FirstOrDefaultAsync(x => x.Id == messageId);

        if (message == null)
            throw new ChatNotFoundException("Message not found");

        await EnsureMember(currentUserId, message.ChatRoomId);

        if (message.SenderId != currentUserId &&
            !await IsSystemAdmin(currentUserId))
        {
            throw new ChatForbiddenException("You are not allowed to delete this message");
        }

        message.IsDeleted = true;
        message.EditedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetMessageDto(message.Id);
    }

    public async Task MarkAsRead(Guid currentUserId, Guid roomId)
    {
        await EnsureMember(currentUserId, roomId);

        var unreadMessages = await _dbContext.ChatMessages
            .Where(message =>
                message.ChatRoomId == roomId &&
                message.SenderId != currentUserId &&
                !message.IsDeleted &&
                !message.Reads.Any(read => read.UserId == currentUserId))
            .Select(message => message.Id)
            .ToListAsync();

        foreach (var messageId in unreadMessages)
        {
            _dbContext.ChatMessageReads.Add(new ChatMessageRead
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                UserId = currentUserId,
                ReadAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task<ChatMessageDto> GetMessageDto(Guid messageId)
    {
        var message = await _dbContext.ChatMessages
            .Include(x => x.Sender)
            .Include(x => x.Reads)
            .ThenInclude(x => x.User)
            .Include(x => x.ReplyToMessage)
            .ThenInclude(x => x!.Sender)
            .FirstOrDefaultAsync(x => x.Id == messageId);

        if (message == null)
            throw new ChatNotFoundException("Message not found");

        return MapMessage(message);
    }

    private async Task EnsureMember(Guid currentUserId, Guid roomId)
    {
        if (!await _chatRoomService.IsActiveMember(currentUserId, roomId))
            throw new ChatForbiddenException("You are not a member of this room");
    }

    private async Task<bool> IsSystemAdmin(Guid userId)
    {
        return await _dbContext.Users.AnyAsync(x =>
            x.Id == userId &&
            x.Role == "Admin" &&
            x.IsActive);
    }

    private static ChatMessageDto MapMessage(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            RoomId = message.ChatRoomId,
            SenderId = message.SenderId,
            SenderName = message.Sender.FullName,
            Content = message.IsDeleted ? "" : message.Content,
            MessageType = message.MessageType,
            AttachmentUrl = message.IsDeleted ? null : message.AttachmentUrl,
            ReplyToMessage = message.ReplyToMessage == null
                ? null
                : new ChatMessageDto
                {
                    Id = message.ReplyToMessage.Id,
                    RoomId = message.ReplyToMessage.ChatRoomId,
                    SenderId = message.ReplyToMessage.SenderId,
                    SenderName = message.ReplyToMessage.Sender.FullName,
                    Content = message.ReplyToMessage.IsDeleted
                        ? ""
                        : message.ReplyToMessage.Content,
                    MessageType = message.ReplyToMessage.MessageType,
                    AttachmentUrl = message.ReplyToMessage.IsDeleted
                        ? null
                        : message.ReplyToMessage.AttachmentUrl,
                    CreatedAt = message.ReplyToMessage.CreatedAt,
                    EditedAt = message.ReplyToMessage.EditedAt,
                    IsDeleted = message.ReplyToMessage.IsDeleted
                },
            CreatedAt = message.CreatedAt,
            EditedAt = message.EditedAt,
            IsDeleted = message.IsDeleted,
            ReadBy = message.Reads
                .Select(read => new ChatReadDto
                {
                    UserId = read.UserId,
                    FullName = read.User.FullName,
                    ReadAt = read.ReadAt
                })
                .ToList()
        };
    }
}
