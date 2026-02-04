namespace server;

public class ChatRoom
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<UserChatRoom> Members { get; set; } = new List<UserChatRoom>();
}