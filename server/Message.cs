namespace server;

public class Message
{
    public string Id { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;

    public string ChatRoomId { get; set; } = null!;
    public ChatRoom ChatRoom { get; set; } = null!;
}