using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiniTaskManagement.Api.DTOs;
using MiniTaskManagement.Api.Services;

namespace MiniTaskManagement.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatRoomService _chatRoomService;
    private readonly IChatMessageService _chatMessageService;

    public ChatHub(
        IChatRoomService chatRoomService,
        IChatMessageService chatMessageService)
    {
        _chatRoomService = chatRoomService;
        _chatMessageService = chatMessageService;
    }

    public async Task JoinRoom(Guid roomId)
    {
        var userId = CurrentUserId();

        if (!await _chatRoomService.IsActiveMember(userId, roomId))
            throw new HubException("Forbidden");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RoomGroup(roomId));
    }

    public async Task LeaveRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RoomGroup(roomId));
    }

    public async Task SendMessage(
        Guid roomId,
        SendMessageRequest request)
    {
        var userId = CurrentUserId();
        var message = await _chatMessageService.SendMessage(
            userId,
            roomId,
            request);

        await Clients
            .Group(RoomGroup(roomId))
            .SendAsync("ReceiveMessage", message);
    }

    public async Task Typing(Guid roomId)
    {
        var userId = CurrentUserId();

        if (!await _chatRoomService.IsActiveMember(userId, roomId))
            throw new HubException("Forbidden");

        await Clients
            .OthersInGroup(RoomGroup(roomId))
            .SendAsync("UserTyping", new
            {
                RoomId = roomId,
                UserId = userId,
                UserName = Context.User?.FindFirstValue(ClaimTypes.Name)
            });
    }

    public async Task MarkAsRead(Guid roomId)
    {
        var userId = CurrentUserId();

        await _chatMessageService.MarkAsRead(userId, roomId);

        await Clients
            .Group(RoomGroup(roomId))
            .SendAsync("MessagesRead", new
            {
                RoomId = roomId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });
    }

    private Guid CurrentUserId()
    {
        return Guid.Parse(
            Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public static string RoomGroup(Guid roomId) => $"chat-room:{roomId}";
}
