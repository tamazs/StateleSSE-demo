namespace server;

public class LoginUserDto
{
    public string Token { get; set; } = null!;
    public User User { get; set; } = null!;
}