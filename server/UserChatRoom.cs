namespace server;

public class UserChatRoom
{
    public string UserId { get; set; } = null!;
    public User User { get; set; } = null!;

    public string ChatRoomId { get; set; } = null!;
    public ChatRoom ChatRoom { get; set; } = null!;
}