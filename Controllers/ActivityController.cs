using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

[ApiController]
[Route("api/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly IHttpContextAccessor _httpContext;


    public ActivityController(IActivityService activityService, IHttpContextAccessor accessor)
    {
        _activityService = activityService;
        _httpContext = accessor;
    }

    private string GetIp()
        => _httpContext.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;

    private string GetUserAgent()
        => Request.Headers["User-Agent"].ToString();

    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] ActivityLog model)
    {
        model.Timestamp = DateTimeOffset.UtcNow;
        model.IpAddress = GetIp();
        model.UserAgent = GetUserAgent();

        await _activityService.LogAsync(model);

        return Ok(new { message = "logged" });
    }
}
