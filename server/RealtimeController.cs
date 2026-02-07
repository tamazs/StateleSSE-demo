using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using StateleSSE.AspNetCore;

namespace server;

[ApiController]
[Route("api/realtime")]
public class RealtimeController(
    AppDbContext dbContext,
    ISseBackplane backplane,
    IConnectionMultiplexer connectionMultiplexer
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
        
        var userId = CurrentUserId;

        if (!string.IsNullOrEmpty(userId))
        {
            var redis = connectionMultiplexer.GetDatabase();
            await redis.StringSetAsync(
                $"connection:{connection.ConnectionId}",
                userId
            );
        }

        await foreach (var evt in connection.ReadAllAsync(HttpContext.RequestAborted))
        {
            await sse.WriteAsync(
                evt.Group ?? "message",
                evt.Data
            );
        }
    }

    /* -------------------- JOIN ROOM -------------------- */
    
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

    [Authorize]
    [HttpPost("join")]
    [ProducesResponseType(typeof(JoinGroupBroadcast), 202)]
    [ProducesResponseType(typeof(JoinGroupResponse), 200)]
    [ProducesResponseType(typeof(UserLeftResponseDto), 400)]
    public async Task<JoinGroupResponse> JoinGroup([FromBody] JoinGroupRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var u =  await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        var room = await dbContext.ChatRooms.FirstOrDefaultAsync(r => r.Id == request.Group) ??
                   throw new ValidationException("Room does not exist");
        var name = u?.Username ?? "Anonymous";
        await backplane.Groups.AddToGroupAsync("nickname/"+request.ConnectionId, name);
        await backplane.Groups.AddToGroupAsync(request.ConnectionId, request.Group);
        var members = await backplane.Groups.GetMembersAsync(request.Group);
        var list = new List<ConnectionIdAndUserName>();
        foreach (var m in members)
        {
            var nickname = await backplane.Groups.GetClientGroupsAsync("nickname/" + m);
            list.Add(new ConnectionIdAndUserName(m, nickname.FirstOrDefault() ?? "Anonymous"));
        }
        await backplane.Clients.SendToGroupAsync(request.Group, new JoinGroupBroadcast(list));
        
        return new JoinGroupResponse(room);

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

    [HttpGet("getmembers")]
    public async Task<List<User>> GetGroupMembers(string groupId)
    {
        return await dbContext.UserChatRooms
            .Where(uc => uc.ChatRoomId == groupId)
            .Select(uc => uc.User)
            .ToListAsync();
    }

    [HttpPost("getonlinemembers")]
    public async Task<List<User>> GetOnlineGroupMembers(string groupId)
    {
        var connectionIds = await backplane.Groups.GetMembersAsync(groupId);

        var redis = connectionMultiplexer.GetDatabase();

        var userIds = new List<string>();

        foreach (var connectionId in connectionIds)
        {
            var userId = await redis.StringGetAsync($"connection:{connectionId}");
            if (!userId.IsNullOrEmpty)
                userIds.Add(userId!);
        }

        return await dbContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .ToListAsync();
    }
    
    [HttpGet(nameof(GetRooms))]
    public async Task<List<ChatRoom>> GetRooms()
        => await dbContext.ChatRooms.ToListAsync();
    
    /* -------------------- SEND MESSAGE -------------------- */

    [Authorize]
    [HttpPost("send")]
    [Produces<MessageResponseDto>]
    public async Task Send([FromBody] SendGroupMessageRequestDto request)
    {
        var userId = CurrentUserId!;
        
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        
        var msg = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Content = request.Message,
            UserId = userId,
            ChatRoomId = request.GroupId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Messages.Add(msg);
        await dbContext.SaveChangesAsync();

        await backplane.Clients.SendToGroupAsync(request.GroupId, new MessageResponseDto
        {
            User = user!.Username,
            Message = request.Message
        });
    }
    
    [Authorize]
    [HttpPost("poke")]
    [ProducesResponseType(typeof(PokeResponseDto), 200)]
    public async Task Poke([FromBody] PokeRequestDto dto)
    {
        var userId = CurrentUserId!;
        var u = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        var name = u?.Username ?? "Anonymous";

        await backplane.Clients.SendToClientAsync(dto.connectionIdToPoke, new PokeResponseDto(name));
    }
}

public record ConnectionResponse(string ConnectionId) : BaseResponseDto;


public record JoinGroupRequest(string ConnectionId, string Group);

public record PokeResponseDto(string pokedBy) : BaseResponseDto;
public record PokeRequestDto(string connectionIdToPoke);

public record SendGroupMessageRequestDto
{
    public string Message { get; set; } = "";
    public string GroupId { get; set; } = "";
}

public record JoinGroupResponse(ChatRoom chatroom) : BaseResponseDto;

public record MessageResponseDto : BaseResponseDto
{
    public string Message { get; set; } = "";
    public string? User { get; set; } = "";
}

public record JoinGroupBroadcast(List<ConnectionIdAndUserName> ConnectedUsers) : BaseResponseDto;

public record ConnectionIdAndUserName(string ConnectionId, string UserName);