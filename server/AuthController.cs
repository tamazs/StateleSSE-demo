using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace server;

[ApiController]
public class AuthController(AppDbContext dbContext ,IConfiguration config)
{
    [HttpPost("register")]
    public async Task<User> RegisterUser(RegisterLoginDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);
        
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == dto.UserName);
        if (existingUser != null) throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = dto.UserName,
            Role = UserRole.User.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, dto.Password);
        
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        return new User();
    }

    [HttpPost("login")]
    public async Task<LoginUserDto> LoginUser(RegisterLoginDto dto)
    {
        Validator.ValidateObject(dto, new ValidationContext(dto), true);
        
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == dto.UserName);
        if (user == null) throw new UnauthorizedAccessException("Invalid username or password");
        
        var verification = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification ==  PasswordVerificationResult.Failed) throw new UnauthorizedAccessException("Invalid username or password");
        
        var token = CreateToken(user);

        return new LoginUserDto
        {
            Token = token,
            User = new User()
        };
    }
    
    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config.GetValue<string>("AppOptions:Token")!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: config.GetValue<string>("AppOptions:Issuer"),
            audience: config.GetValue<string>("AppOptions:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}