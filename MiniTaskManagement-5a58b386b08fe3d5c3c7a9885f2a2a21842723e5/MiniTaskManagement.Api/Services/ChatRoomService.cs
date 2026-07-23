using Microsoft.EntityFrameworkCore;
using MiniTaskManagement.Api.Data;
using MiniTaskManagement.Api.DTOs;
using MiniTaskManagement.Api.Entities;

namespace MiniTaskManagement.Api.Services;

public class ChatRoomService : IChatRoomService
{
    private readonly AppDbContext _dbContext;

    public ChatRoomService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ChatUserDto>> GetChatUsers(Guid currentUserId)
    {
        return await _dbContext.Users
            .Where(x => x.IsActive && x.Id != currentUserId)
            .OrderBy(x => x.FullName)
            .Select(x => new ChatUserDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role
            })
            .ToListAsync();
    }

    public async Task<List<ChatRoomDto>> GetRooms(Guid currentUserId)
    {
        var rooms = await _dbContext.ChatRooms
            .Where(room =>
                !room.IsDeleted &&
                room.Members.Any(member =>
                    member.UserId == currentUserId &&
                    !member.IsRemoved &&
                    member.LeftAt == null))
            .Include(room => room.Members)
            .ThenInclude(member => member.User)
            .Include(room => room.Messages)
            .ThenInclude(message => message.Sender)
            .Include(room => room.Messages)
            .ThenInclude(message => message.Reads)
            .ThenInclude(read => read.User)
            .AsSplitQuery()
            .ToListAsync();

        return rooms
            .OrderByDescending(room =>
                room.Messages
                    .Where(message => !message.IsDeleted)
                    .Select(message => (DateTime?)message.CreatedAt)
                    .Max() ?? room.CreatedAt)
            .Select(room => MapRoom(room, currentUserId))
            .ToList();
    }

    public async Task<ChatRoomDto> GetOrCreatePrivateRoom(
        Guid currentUserId,
        Guid otherUserId)
    {
        if (currentUserId == otherUserId)
            throw new ChatForbiddenException("Cannot create private chat with yourself");

        await EnsureActiveUser(otherUserId);

        var room = await _dbContext.ChatRooms
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .ThenInclude(x => x.Sender)
            .FirstOrDefaultAsync(room =>
                !room.IsDeleted &&
                room.Type == ChatRoomType.Private &&
                room.Members.Count(member =>
                    !member.IsRemoved &&
                    member.LeftAt == null) == 2 &&
                room.Members.Any(member =>
                    member.UserId == currentUserId &&
                    !member.IsRemoved &&
                    member.LeftAt == null) &&
                room.Members.Any(member =>
                    member.UserId == otherUserId &&
                    !member.IsRemoved &&
                    member.LeftAt == null));

        if (room != null)
            return MapRoom(room, currentUserId);

        room = CreateRoom(
            currentUserId,
            ChatRoomType.Private,
            null,
            null,
            null);

        AddMemberEntity(room, currentUserId, ChatRoomMemberRole.Owner);
        AddMemberEntity(room, otherUserId, ChatRoomMemberRole.Member);

        _dbContext.ChatRooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task<ChatRoomDto> GetOrCreateSupportRoom(
        Guid currentUserId,
        Guid? adminId)
    {
        var currentUser = await GetUser(currentUserId);
        var adminIds = new List<Guid>();

        if (adminId.HasValue)
        {
            var admin = await GetUser(adminId.Value);

            if (admin.Role != "Admin")
                throw new ChatForbiddenException("Selected user is not an admin");

            adminIds.Add(admin.Id);
        }
        else
        {
            adminIds = await _dbContext.Users
                .Where(x => x.IsActive && x.Role == "Admin")
                .Select(x => x.Id)
                .ToListAsync();
        }

        if (currentUser.Role != "Admin")
        {
            adminIds = adminIds
                .Where(id => id != currentUserId)
                .Distinct()
                .ToList();

            if (adminIds.Count == 0)
                throw new ChatForbiddenException("No admin is available for support chat");
        }

        var query = _dbContext.ChatRooms
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .ThenInclude(x => x.Sender)
            .Where(room =>
                !room.IsDeleted &&
                room.Type == ChatRoomType.Support &&
                room.Members.Any(member =>
                    member.UserId == currentUserId &&
                    !member.IsRemoved &&
                    member.LeftAt == null));

        if (adminId.HasValue)
        {
            query = query.Where(room =>
                room.Members.Any(member =>
                    member.UserId == adminId.Value &&
                    !member.IsRemoved &&
                    member.LeftAt == null));
        }

        var existing = await query.FirstOrDefaultAsync();

        if (existing != null)
            return MapRoom(existing, currentUserId);

        var room = CreateRoom(
            currentUserId,
            ChatRoomType.Support,
            "Hỗ trợ",
            null,
            null);

        AddMemberEntity(room, currentUserId, ChatRoomMemberRole.Owner);

        foreach (var id in adminIds.Distinct())
        {
            AddMemberEntity(room, id, ChatRoomMemberRole.Admin);
        }

        _dbContext.ChatRooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task<ChatRoomDto> CreateGroupRoom(
        Guid currentUserId,
        CreateGroupChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Room name is required");

        var memberIds = request.MemberIds
            .Where(id => id != currentUserId)
            .Distinct()
            .ToList();

        var activeMemberCount = await _dbContext.Users
            .CountAsync(user =>
                memberIds.Contains(user.Id) &&
                user.IsActive);

        if (activeMemberCount != memberIds.Count)
            throw new ChatForbiddenException("One or more members are invalid");

        var room = CreateRoom(
            currentUserId,
            ChatRoomType.Group,
            request.Name.Trim(),
            null,
            null);

        AddMemberEntity(room, currentUserId, ChatRoomMemberRole.Owner);

        foreach (var id in memberIds)
        {
            AddMemberEntity(room, id, ChatRoomMemberRole.Member);
        }

        _dbContext.ChatRooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task<ChatRoomDto> GetOrCreateProjectRoom(
        Guid currentUserId,
        Guid projectId)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
            throw new ChatNotFoundException("Project not found");

        if (project.OwnerId != currentUserId && !await IsSystemAdmin(currentUserId))
            throw new ChatForbiddenException("You are not allowed to access this project chat");

        var existing = await _dbContext.ChatRooms
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .ThenInclude(x => x.Sender)
            .FirstOrDefaultAsync(room =>
                !room.IsDeleted &&
                room.Type == ChatRoomType.Project &&
                room.ProjectId == projectId);

        if (existing != null)
        {
            await EnsureRoomMember(existing, currentUserId, ChatRoomMemberRole.Admin);
            await _dbContext.SaveChangesAsync();
            return await GetRoomDto(existing.Id, currentUserId);
        }

        var room = CreateRoom(
            currentUserId,
            ChatRoomType.Project,
            project.Name,
            project.Id,
            null);

        AddMemberEntity(room, project.OwnerId, ChatRoomMemberRole.Owner);

        if (currentUserId != project.OwnerId)
            AddMemberEntity(room, currentUserId, ChatRoomMemberRole.Admin);

        _dbContext.ChatRooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task<ChatRoomDto> GetOrCreateTaskRoom(
        Guid currentUserId,
        Guid taskId)
    {
        var task = await _dbContext.Tasks
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
            throw new ChatNotFoundException("Task not found");

        var allowedUserIds = new HashSet<Guid> { task.UserId };

        if (task.Project != null)
            allowedUserIds.Add(task.Project.OwnerId);

        var systemAdmin = await IsSystemAdmin(currentUserId);

        if (!allowedUserIds.Contains(currentUserId) && !systemAdmin)
            throw new ChatForbiddenException("You are not allowed to access this task chat");

        var existing = await _dbContext.ChatRooms
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .ThenInclude(x => x.Sender)
            .FirstOrDefaultAsync(room =>
                !room.IsDeleted &&
                room.Type == ChatRoomType.Task &&
                room.TaskId == taskId);

        if (existing != null)
        {
            await EnsureRoomMember(existing, currentUserId, systemAdmin ? ChatRoomMemberRole.Admin : ChatRoomMemberRole.Member);
            await _dbContext.SaveChangesAsync();
            return await GetRoomDto(existing.Id, currentUserId);
        }

        var room = CreateRoom(
            currentUserId,
            ChatRoomType.Task,
            task.Title,
            task.ProjectId,
            task.Id);

        foreach (var id in allowedUserIds)
        {
            AddMemberEntity(
                room,
                id,
                id == task.UserId ? ChatRoomMemberRole.Owner : ChatRoomMemberRole.Admin);
        }

        if (!allowedUserIds.Contains(currentUserId))
            AddMemberEntity(room, currentUserId, ChatRoomMemberRole.Admin);

        _dbContext.ChatRooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task<ChatRoomDto> AddMember(
        Guid currentUserId,
        Guid roomId,
        AddMemberRequest request)
    {
        var room = await GetRoomForManage(currentUserId, roomId);
        await EnsureActiveUser(request.UserId);

        await EnsureRoomMember(room, request.UserId, request.Role);
        room.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task<ChatRoomDto> RemoveMember(
        Guid currentUserId,
        Guid roomId,
        Guid userId)
    {
        var room = await GetRoomForManage(currentUserId, roomId);

        var member = room.Members
            .FirstOrDefault(x =>
                x.UserId == userId &&
                !x.IsRemoved &&
                x.LeftAt == null);

        if (member == null)
            throw new ChatNotFoundException("Member not found");

        if (member.Role == ChatRoomMemberRole.Owner)
        {
            var ownerCount = room.Members.Count(x =>
                x.Role == ChatRoomMemberRole.Owner &&
                !x.IsRemoved &&
                x.LeftAt == null);

            if (ownerCount <= 1)
                throw new ChatForbiddenException("Cannot remove the last owner");
        }

        member.IsRemoved = true;
        member.LeftAt = DateTime.UtcNow;
        room.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task<ChatRoomDto> UpdateRoom(
        Guid currentUserId,
        Guid roomId,
        UpdateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Room name is required");

        var room = await GetRoomForManage(currentUserId, roomId);

        room.Name = request.Name.Trim();
        room.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return await GetRoomDto(room.Id, currentUserId);
    }

    public async Task DeleteRoom(Guid currentUserId, Guid roomId)
    {
        var room = await _dbContext.ChatRooms
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == roomId && !x.IsDeleted);

        if (room == null)
            throw new ChatNotFoundException("Room not found");

        var isOwner = room.Members.Any(member =>
            member.UserId == currentUserId &&
            member.Role == ChatRoomMemberRole.Owner &&
            !member.IsRemoved &&
            member.LeftAt == null);

        if (!isOwner && !await IsSystemAdmin(currentUserId))
            throw new ChatForbiddenException("You are not allowed to delete this room");

        room.IsDeleted = true;
        room.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsActiveMember(Guid userId, Guid roomId)
    {
        return await _dbContext.ChatRoomMembers.AnyAsync(member =>
            member.ChatRoomId == roomId &&
            member.UserId == userId &&
            !member.IsRemoved &&
            member.LeftAt == null &&
            !member.ChatRoom.IsDeleted);
    }

    public async Task<List<Guid>> GetActiveMemberIds(Guid roomId)
    {
        return await _dbContext.ChatRoomMembers
            .Where(member =>
                member.ChatRoomId == roomId &&
                !member.IsRemoved &&
                member.LeftAt == null)
            .Select(member => member.UserId)
            .ToListAsync();
    }

    private async Task<ChatRoom> GetRoomForManage(Guid currentUserId, Guid roomId)
    {
        var room = await _dbContext.ChatRooms
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == roomId && !x.IsDeleted);

        if (room == null)
            throw new ChatNotFoundException("Room not found");

        var canManage = room.Members.Any(member =>
            member.UserId == currentUserId &&
            (member.Role == ChatRoomMemberRole.Owner ||
                member.Role == ChatRoomMemberRole.Admin) &&
            !member.IsRemoved &&
            member.LeftAt == null);

        if (!canManage && !await IsSystemAdmin(currentUserId))
            throw new ChatForbiddenException("You are not allowed to manage this room");

        return room;
    }

    private async Task EnsureRoomMember(
        ChatRoom room,
        Guid userId,
        ChatRoomMemberRole role)
    {
        var existing = room.Members.FirstOrDefault(x => x.UserId == userId);

        if (existing != null)
        {
            existing.Role = role;
            existing.IsRemoved = false;
            existing.LeftAt = null;
            return;
        }

        await EnsureActiveUser(userId);
        AddMemberEntity(room, userId, role);
    }

    private static ChatRoom CreateRoom(
        Guid currentUserId,
        ChatRoomType type,
        string? name,
        Guid? projectId,
        Guid? taskId)
    {
        return new ChatRoom
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = currentUserId,
            Type = type,
            Name = name,
            ProjectId = projectId,
            TaskId = taskId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void AddMemberEntity(
        ChatRoom room,
        Guid userId,
        ChatRoomMemberRole role)
    {
        if (room.Members.Any(member => member.UserId == userId))
            return;

        room.Members.Add(new ChatRoomMember
        {
            Id = Guid.NewGuid(),
            ChatRoomId = room.Id,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        });
    }

    private async Task<ChatRoomDto> GetRoomDto(Guid roomId, Guid currentUserId)
    {
        var room = await _dbContext.ChatRooms
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .Include(x => x.Messages)
            .ThenInclude(x => x.Sender)
            .Include(x => x.Messages)
            .ThenInclude(x => x.Reads)
            .ThenInclude(x => x.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == roomId && !x.IsDeleted);

        if (room == null)
            throw new ChatNotFoundException("Room not found");

        return MapRoom(room, currentUserId);
    }

    private ChatRoomDto MapRoom(ChatRoom room, Guid currentUserId)
    {
        var lastMessage = room.Messages
            .Where(message => !message.IsDeleted)
            .OrderByDescending(message => message.CreatedAt)
            .FirstOrDefault();

        return new ChatRoomDto
        {
            Id = room.Id,
            Name = ResolveRoomName(room, currentUserId),
            Type = room.Type,
            CreatedAt = room.CreatedAt,
            LastMessage = lastMessage == null ? null : MapMessage(lastMessage),
            UnreadCount = room.Messages.Count(message =>
                !message.IsDeleted &&
                message.SenderId != currentUserId &&
                !message.Reads.Any(read => read.UserId == currentUserId)),
            Members = room.Members
                .Where(member =>
                    !member.IsRemoved &&
                    member.LeftAt == null)
                .OrderBy(member => member.User.FullName)
                .Select(member => new ChatMemberDto
                {
                    UserId = member.UserId,
                    FullName = member.User.FullName,
                    AvatarUrl = null,
                    Role = member.Role,
                    IsOnline = false
                })
                .ToList()
        };
    }

    private static string? ResolveRoomName(ChatRoom room, Guid currentUserId)
    {
        if (!string.IsNullOrWhiteSpace(room.Name))
            return room.Name;

        if (room.Type == ChatRoomType.Private)
        {
            return room.Members
                .Where(member =>
                    member.UserId != currentUserId &&
                    !member.IsRemoved &&
                    member.LeftAt == null)
                .Select(member => member.User.FullName)
                .FirstOrDefault();
        }

        return room.Type.ToString();
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

    private async Task<User> GetUser(Guid userId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user == null)
            throw new ChatNotFoundException("User not found");

        return user;
    }

    private async Task EnsureActiveUser(Guid userId)
    {
        _ = await GetUser(userId);
    }

    private async Task<bool> IsSystemAdmin(Guid userId)
    {
        return await _dbContext.Users.AnyAsync(x =>
            x.Id == userId &&
            x.Role == "Admin" &&
            x.IsActive);
    }
}
