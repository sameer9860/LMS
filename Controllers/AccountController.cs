using Microsoft.AspNetCore.Mvc;
// using LMS.Data; // Ensure this is the correct namespace for ApplicationDbContext
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using LMS.Views.Data;
using LMS.Models;
namespace LMS.Controllers
{



    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LMS.ViewModels.LoginViewModel.LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .FirstOrDefault(u => u.Username == model.Username && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            // Create claims for cookie authentication
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
        new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
        new Claim("UserId", user.Id.ToString())
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 🔥 Add Activity Log for Login
          var activity = new ActivityLog
    {
        UserId = user.Id.ToString(),
        ActivityType = ActivityType.Login,
        Timestamp = DateTimeOffset.UtcNow,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = Request.Headers["User-Agent"].ToString(),
        MetadataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            Message = "User successfully logged in",
            Username = user.Username
        })
    };

    _context.ActivityLogs.Add(activity);
    await _context.SaveChangesAsync();
            // 🔥 END LOG

            // Redirect based on role
            return user.Role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Instructor" => RedirectToAction("Dashboard", "Instructor"),
                "Student" => RedirectToAction("Dashboard", "Student"),
                _ => RedirectToAction("Login")
            };
        }


        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
