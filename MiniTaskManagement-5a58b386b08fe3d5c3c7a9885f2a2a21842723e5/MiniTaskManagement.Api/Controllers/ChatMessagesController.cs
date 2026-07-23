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
public class ChatMessagesController : ControllerBase
{
    private readonly IChatMessageService _chatMessageService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatMessagesController(
        IChatMessageService chatMessageService,
        IHubContext<ChatHub> hubContext)
    {
        _chatMessageService = chatMessageService;
        _hubContext = hubContext;
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        return await Handle(async () =>
            await _chatMessageService.GetMessages(
                CurrentUserId(),
                roomId,
                page,
                pageSize));
    }

    [HttpPost("rooms/{roomId}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid roomId,
        SendMessageRequest request)
    {
        return await Handle(async () =>
        {
            var message = await _chatMessageService.SendMessage(
                CurrentUserId(),
                roomId,
                request);

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(roomId))
                .SendAsync("ReceiveMessage", message);

            return message;
        });
    }

    [HttpPatch("messages/{messageId}")]
    public async Task<IActionResult> UpdateMessage(
        Guid messageId,
        SendMessageRequest request)
    {
        return await Handle(async () =>
        {
            var message = await _chatMessageService.UpdateMessage(
                CurrentUserId(),
                messageId,
                request);

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(message.RoomId))
                .SendAsync("MessageUpdated", message);

            return message;
        });
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        return await Handle(async () =>
        {
            var message = await _chatMessageService.DeleteMessage(
                CurrentUserId(),
                messageId);

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(message.RoomId))
                .SendAsync("MessageDeleted", message);

            return message;
        });
    }

    [HttpPost("rooms/{roomId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid roomId)
    {
        return await Handle(async () =>
        {
            var userId = CurrentUserId();
            await _chatMessageService.MarkAsRead(userId, roomId);

            var payload = new
            {
                RoomId = roomId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(ChatHub.RoomGroup(roomId))
                .SendAsync("MessagesRead", payload);

            return payload;
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
