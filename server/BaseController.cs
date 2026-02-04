using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace server;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected string? CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}