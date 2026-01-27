using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using LMS.Views.Data;
using LMS.Models;
using LMS.ViewModels;

namespace LMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============================
        // GET: /Account/Login
        // ============================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ============================
        // POST: /Account/Login
        // ============================
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

            // ============================
            // Create Claims
            // ============================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim("UserId", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            // ============================
            // 🔥 Activity Log (ONLY FOR STUDENTS)
            // ============================
            if (user.Role == "Student")
            {
                var activity = new ActivityLog
                {
                    UserId = user.Id.ToString(),
                    ActivityType = ActivityType.Login,
                    Timestamp = DateTimeOffset.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    MetadataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        Message = "Student logged in successfully",
                        Username = user.Username
                    })
                };

                _context.ActivityLogs.Add(activity);
                await _context.SaveChangesAsync();
            }

            // ============================
            // Redirect Based on Role
            // ============================
            return user.Role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Instructor" => RedirectToAction("Dashboard", "Instructor"),
                "Student" => RedirectToAction("Dashboard", "Student"),
                _ => RedirectToAction("Login")
            };
        }

        // ============================
        // GET: /Account/Profile
        // ============================
        [HttpGet]
        public IActionResult Profile()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return role switch
            {
                "Admin" => RedirectToAction("Profile", "Admin"),
                "Instructor" => RedirectToAction("Profile", "Instructor"),
                "Student" => RedirectToAction("Profile", "Student"),
                _ => RedirectToAction("Login")
            };
        }

        // ============================
        // GET: /Account/Settings
        // ============================
        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        // ============================
        // POST: /Account/ChangePassword
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Settings", model);

            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
                return Unauthorized();

            if (user.Password != model.OldPassword)
            {
                ModelState.AddModelError("OldPassword", "The current password you entered is incorrect.");
                return View("Settings", model);
            }

            user.Password = model.NewPassword;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been changed successfully!";
            TempData["PasswordChanged"] = true; // Flag for JS redirection
            return RedirectToAction("Settings");
        }

        // ============================
        // GET: /Account/Logout
        // ============================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }
    }
}
