using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StateleSSE.AspNetCore;

namespace server;

[ApiController]
[Route("api/realtime")]
public class RealtimeController(
    AppDbContext dbContext,
    ISseBackplane backplane
) : BaseController
{
    /* -------------------- CONNECT -------------------- */

    [HttpGet("connect")]
    [Produces<ConnectionResponse>]
    public async Task Connect()
    {
        await using var sse = await HttpContext.OpenSseStreamAsync();
        await using var connection = backplane.CreateConnection();

        await sse.WriteAsync(
            "ConnectionResponse",
            JsonSerializer.Serialize(new
            {
                connectionId = connection.ConnectionId,
                eventType = "ConnectionResponse"
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        );

        await foreach (var evt in connection.ReadAllAsync(HttpContext.RequestAborted))
        {
            await sse.WriteAsync(
                evt.Group ?? "message",
                evt.Data
            );
        }
    }

    /* -------------------- JOIN ROOM -------------------- */

    [HttpPost("join")]
    [Produces<JoinGroupResponse>]
    public async Task Join([FromBody] JoinGroupRequest request)
    {
        await backplane.Groups.AddToGroupAsync(request.ConnectionId, request.Group);

        var members = await backplane.Groups.GetMembersAsync(request.Group);

        await backplane.Clients.SendToGroupAsync(request.Group,  new JoinGroupResponse()
        {
            Members = members.ToList()
        });
    }

    [HttpPost("createroom")]
    public async Task<ChatRoom> CreateChatRoom(string name)
    {
        var chatRoom = new ChatRoom
        {
            Id = Guid.NewGuid().ToString(),
            Members = [],
            Messages = [],
            Name = name
        };
        
        dbContext.ChatRooms.Add(chatRoom);
        await dbContext.SaveChangesAsync();
        
        return chatRoom;
    }

    /* -------------------- SEND MESSAGE -------------------- */

    //[Authorize]
    [HttpPost("send")]
    [Produces<MessageResponseDto>]
    public async Task Send([FromBody] SendGroupMessageRequestDto request)
    {
        var userId = CurrentUserId!;
        
        var msg = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Content = request.Message,
            UserId = "6f2e8c6d-f5e9-4ef2-a8e0-fed23a85f9e7",
            ChatRoomId = request.GroupId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Messages.Add(msg);
        await dbContext.SaveChangesAsync();

        await backplane.Clients.SendToGroupAsync("randomroom", new MessageResponseDto
        {
            User = "6f2e8c6d-f5e9-4ef2-a8e0-fed23a85f9e7",
            Message = request.Message
        });
    }

    /* -------------------- LEAVE ROOM -------------------- */

    [HttpPost("leave")]
    public async Task Leave([FromBody] JoinGroupRequest request)
    {
        await backplane.Groups.RemoveFromGroupAsync(request.ConnectionId, request.Group);

        await backplane.Clients.SendToGroupAsync(request.Group,  new JoinGroupResponse());
    }
}

public record ConnectionResponse(string ConnectionId) : BaseResponseDto;


public record JoinGroupRequest(string ConnectionId, string Group);

public record SendGroupMessageRequestDto
{
    public string Message { get; set; } = "";
    public string GroupId { get; set; } = "";
}

public record JoinGroupResponse : BaseResponseDto
{
    public List<string> Members { get; set; }
}



public record MessageResponseDto : BaseResponseDto
{
    public string Message { get; set; } = "";
    public string? User { get; set; } = "";
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token);
