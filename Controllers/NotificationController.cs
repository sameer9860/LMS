using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LMS.Models;
using LMS.Views.Data; // Ensure this is the correct namespace for ApplicationDbContext
[Authorize]
public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetUserNotifications()
    {
        var username = User.Identity?.Name;
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return Unauthorized();

        var notifications = _context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Title,
                n.Message,
                n.CreatedAt,
                n.ActionUrl,
                n.IconClass
            })
            .ToList();

        return Json(notifications);
    }

  [HttpPost]
public IActionResult MarkAllAsRead()
{
    var username = User.Identity?.Name;
    var user = _context.Users.FirstOrDefault(u => u.Username == username);

    if (user == null)
        return Unauthorized();

    var notifications = _context.Notifications
        .Where(n => n.UserId == user.Id && !n.IsRead)
        .ToList();

    foreach (var n in notifications)
    {
        n.IsRead = true;
    }

    _context.SaveChanges();
    return Ok();
}


    public IActionResult Index()
    {
        var username = User.Identity?.Name;
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return Unauthorized();

        var notifications = _context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        return View(notifications);
    }
}
