using LMS.Hubs;
using LMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using LMS.Views.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    [HttpPost("SaveMessage")]
    public async Task<IActionResult> SaveMessage([FromBody] ChatMessageDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { success = false, error = "Invalid message." });

        var username = User.Identity?.Name ?? "Guest";
        var user = _dbContext.Users.FirstOrDefault(u => u.Username == username);
        if (user == null)
            return Unauthorized(new { success = false });

        var chatMessage = new ChatMessage
        {
            UserId = user.Id,
            CourseId = dto.CourseId,
            Message = dto.Message,
            SentAt = DateTime.Now
        };

        _dbContext.ChatMessages.Add(chatMessage);
        await _dbContext.SaveChangesAsync();

        // Only return success, let client call SignalR to broadcast
        return Ok(new
        {
            success = true,
            user = user.Username,
            message = chatMessage.Message,
            time = chatMessage.SentAt.ToShortTimeString()
        });
    }
}

public class ChatMessageDto
{
    public int CourseId { get; set; }
    public string? Message { get; set; }
}
