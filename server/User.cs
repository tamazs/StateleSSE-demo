namespace server;

public class User
{
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<UserChatRoom> ChatRooms { get; set; } = new List<UserChatRoom>();
}