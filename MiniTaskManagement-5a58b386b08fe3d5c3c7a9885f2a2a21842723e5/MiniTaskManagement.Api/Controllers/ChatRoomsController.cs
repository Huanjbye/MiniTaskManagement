using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiniTaskManagement.Api.DTOs;
using MiniTaskManagement.Api.Hubs;
using MiniTaskManagement.Api.Services;

namespace MiniTaskManagement.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatRoomsController : ControllerBase
{
    private readonly IChatRoomService _chatRoomService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatRoomsController(
        IChatRoomService chatRoomService,
        IHubContext<ChatHub> hubContext)
    {
        _chatRoomService = chatRoomService;
        _hubContext = hubContext;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(await _chatRoomService.GetChatUsers(CurrentUserId()));
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        return Ok(await _chatRoomService.GetRooms(CurrentUserId()));
    }

    [HttpPost("private/{userId}")]
    public async Task<IActionResult> GetOrCreatePrivate(Guid userId)
    {
        return await Handle(async () =>
            await _chatRoomService.GetOrCreatePrivateRoom(CurrentUserId(), userId));
    }

    [HttpPost("support")]
    public async Task<IActionResult> GetOrCreateSupport([FromQuery] Guid? adminId)
    {
        return await Handle(async () =>
            await _chatRoomService.GetOrCreateSupportRoom(CurrentUserId(), adminId));
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup(CreateGroupChatRequest request)
    {
        return await Handle(async () =>
            await _chatRoomService.CreateGroupRoom(CurrentUserId(), request));
    }

    [HttpPost("project/{projectId}")]
    public async Task<IActionResult> GetOrCreateProject(Guid projectId)
    {
        return await Handle(async () =>
            await _chatRoomService.GetOrCreateProjectRoom(CurrentUserId(), projectId));
    }

    [HttpPost("task/{taskId}")]
    public async Task<IActionResult> GetOrCreateTask(Guid taskId)
    {
        return await Handle(async () =>
            await _chatRoomService.GetOrCreateTaskRoom(CurrentUserId(), taskId));
    }

    [HttpPost("rooms/{roomId}/members")]
    public async Task<IActionResult> AddMember(
        Guid roomId,
        AddMemberRequest request)
    {
        return await Handle(async () =>
        {
            var room = await _chatRoomService.AddMember(
                CurrentUserId(),
                roomId,
                request);

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(roomId))
                .SendAsync("MemberAdded", room);

            return room;
        });
    }

    [HttpDelete("rooms/{roomId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid roomId, Guid userId)
    {
        return await Handle(async () =>
        {
            var room = await _chatRoomService.RemoveMember(
                CurrentUserId(),
                roomId,
                userId);

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(roomId))
                .SendAsync("MemberRemoved", new
                {
                    RoomId = roomId,
                    UserId = userId,
                    Room = room
                });

            return room;
        });
    }

    [HttpPatch("rooms/{roomId}")]
    public async Task<IActionResult> UpdateRoom(
        Guid roomId,
        UpdateRoomRequest request)
    {
        return await Handle(async () =>
        {
            var room = await _chatRoomService.UpdateRoom(
                CurrentUserId(),
                roomId,
                request);

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(roomId))
                .SendAsync("RoomUpdated", room);

            return room;
        });
    }

    [HttpDelete("rooms/{roomId}")]
    public async Task<IActionResult> DeleteRoom(Guid roomId)
    {
        return await Handle(async () =>
        {
            await _chatRoomService.DeleteRoom(CurrentUserId(), roomId);

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(roomId))
                .SendAsync("RoomUpdated", new
                {
                    Id = roomId,
                    IsDeleted = true
                });

            return new { Id = roomId };
        });
    }

    private async Task<IActionResult> Handle<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (ChatForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (ChatNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private Guid CurrentUserId()
    {
        return Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
