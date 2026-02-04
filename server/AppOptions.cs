using System.ComponentModel.DataAnnotations;

namespace server;

public class AppOptions
{
    [Required] [MinLength(1)] public string DbConnectionString { get; set; }
    [Required] [MinLength(1)] public string RenderConnectionString { get; set; }
    [Required] [MinLength(1)] public string Token { get; set; }
    [Required] [MinLength(1)] public string Issuer { get; set; }
    [Required] [MinLength(1)] public string Audience { get; set; }
}