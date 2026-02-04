using System.ComponentModel.DataAnnotations;

namespace server;

public class RegisterLoginDto
{
    [MinLength(3)] [Required]
    public string UserName { get; set; } = string.Empty;
    
    [MinLength(6)] [Required]
    public string Password { get; set; } = string.Empty;
}