using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using LMS.Views.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Models;
using LMS.ViewModels;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public AdminController(ApplicationDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }




   public IActionResult Dashboard()
{
    var username = User.Identity!.Name;

    if (string.IsNullOrEmpty(username))
    {
        return Unauthorized();
    }

    // Find logged-in user (assuming this is an admin/superadmin)
    var currentUser = _dbContext.Users.FirstOrDefault(u => u.Username == username);

    if (currentUser == null)
    {
        return Unauthorized();
    }

    // Count all instructors and students (since admin/superadmin oversees all)
    int instructorCount = _dbContext.Users.Count(u => u.Role == "Instructor");
    int studentCount = _dbContext.Users.Count(u => u.Role == "Student");

    // If you want to count only users created by this admin (if you have a CreatedBy field)
    // int instructorCount = _dbContext.Users.Count(u => u.Role == "Instructor" && u.CreatedBy == currentUser.Id);
    // int studentCount = _dbContext.Users.Count(u => u.Role == "Student" && u.CreatedBy == currentUser.Id);

    ViewBag.InstructorCount = instructorCount;
    ViewBag.StudentCount = studentCount;

    return View();
}

    // GET: Show form to create instructor
    [HttpGet]
    public IActionResult CreateInstructor()
    {
        return View();
    }

   

    // POST: Handle form submission
    [HttpPost]
    public async Task<IActionResult> CreateInstructor(InstructorViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // 1. Create User
        var user = new User
        {
            Username = model.Username,
            Password = model.Password, // In production, hash the password!
            Role = "Instructor"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // 2. Create Instructor and link to User
        var instructor = new Instructor
        {
            FirstName = model.FirstName,
            MiddleName = model.MiddleName,
            LastName = model.LastName,
            Email = model.Email,
            Gender = model.Gender,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth,
            Qualification = model.Qualification,
            Expertise = model.Expertise,
            YearsOfExperience = model.YearsOfExperience,
            Bio = model.Bio,
            ProfileImagePath = null, // Will be set after file upload
            UserId = user.Id // Link to User
        };

        if (model.ProfileImage != null && model.ProfileImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "instructors");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Path.GetRandomFileName() + Path.GetExtension(model.ProfileImage.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfileImage.CopyToAsync(stream);
            }

            instructor.ProfileImagePath = Path.Combine("uploads", "instructors", fileName).Replace("\\", "/");
        }

        _dbContext.Instructors.Add(instructor);
        await _dbContext.SaveChangesAsync();

        // Create notification for the newly created instructor
        var notification = new Notification
        {
            UserId = user.Id,
            Title = "Welcome to LMS",
            Message = $"Your instructor account has been created. Your username is: {model.Username}. Please log in and create your first course!",
            NotificationType = "instructor_created",
            RelatedId = instructor.id,
            IconClass = "fas fa-user-tie",
            ActionUrl = "/Instructor/CreateCourse",
            CreatedAt = DateTime.Now,
            IsRead = false
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Instructor created successfully!";
        return RedirectToAction("InstructorList");
    }

    [HttpGet]
    public IActionResult InstructorList(InstructorFilterViewModel filter)
    {
        // Include User so you can access Username in the view
        var query = _dbContext.Instructors.Include(i => i.User).AsQueryable();

        // Search by name (FirstName, MiddleName, LastName)
        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            var searchQuery = filter.SearchQuery.ToLower();
            query = query.Where(i => 
                i.FirstName.ToLower().Contains(searchQuery) ||
                i.MiddleName.ToLower().Contains(searchQuery) ||
                i.LastName.ToLower().Contains(searchQuery));
        }

        // Filter by email
        if (!string.IsNullOrEmpty(filter.Email))
        {
            query = query.Where(i => i.Email.ToLower().Contains(filter.Email.ToLower()));
        }

        // Filter by phone number
        if (!string.IsNullOrEmpty(filter.PhoneNumber))
        {
            query = query.Where(i => i.PhoneNumber.Contains(filter.PhoneNumber));
        }

        // Filter by qualification
        if (!string.IsNullOrEmpty(filter.Qualification))
        {
            query = query.Where(i => i.Qualification.ToLower().Contains(filter.Qualification.ToLower()));
        }

        var instructors = query.OrderBy(i => i.FirstName).ToList();

        var viewModel = new InstructorFilterViewModel
        {
            SearchQuery = filter.SearchQuery,
            Email = filter.Email,
            PhoneNumber = filter.PhoneNumber,
            Qualification = filter.Qualification,
            Instructors = instructors
        };

        return View(viewModel);
    }

    // GET: Edit instructor
    [HttpGet]
    public async Task<IActionResult> EditInstructor(int id)
    {
        var instructor = await _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.id == id);

        if (instructor == null) return NotFound();

        var model = new InstructorViewModel
        {
            FirstName = instructor.FirstName ?? string.Empty,
            MiddleName = instructor.MiddleName ?? string.Empty,
            LastName = instructor.LastName ?? string.Empty,
            Email = instructor.Email ?? string.Empty,
            Gender = instructor.Gender ?? string.Empty,
            PhoneNumber = instructor.PhoneNumber ?? string.Empty,
            DateOfBirth = instructor.DateOfBirth,
            Qualification = instructor.Qualification ?? string.Empty,
            Expertise = instructor.Expertise ?? string.Empty,
            YearsOfExperience = instructor.YearsOfExperience,
            Bio = instructor.Bio ?? string.Empty,
            Username = instructor.User?.Username ?? string.Empty,
            //ProfileImage = instructor.ProfileImage,
            // Do not pre-fill password fields for security
        };

        return View(model);
    }

    // POST: Edit instructor
    [HttpPost]
    public async Task<IActionResult> EditInstructor(int id, InstructorViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var instructor = await _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.id == id);

        if (instructor == null) return NotFound();

        // Update user info
        if (instructor.User != null)
        {
            instructor.User.Username = model.Username;
            if (!string.IsNullOrEmpty(model.Password))
            {
                instructor.User.Password = model.Password; // hash in prod
            }
        }

        // Update instructor fields
        instructor.FirstName = model.FirstName;
        instructor.MiddleName = model.MiddleName;
        instructor.LastName = model.LastName;
        instructor.Email = model.Email;
        instructor.Gender = model.Gender;
        instructor.PhoneNumber = model.PhoneNumber;
        instructor.DateOfBirth = model.DateOfBirth;
        instructor.Qualification = model.Qualification;
        instructor.Expertise = model.Expertise;
        instructor.YearsOfExperience = model.YearsOfExperience;
        instructor.Bio = model.Bio;

        // Handle profile image update
        if (model.ProfileImage != null && model.ProfileImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "instructors");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Path.GetRandomFileName() + Path.GetExtension(model.ProfileImage.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfileImage.CopyToAsync(stream);
            }

            instructor.ProfileImagePath = Path.Combine("uploads", "instructors", fileName).Replace("\\", "/");
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Instructor updated successfully!";
        return RedirectToAction("InstructorList");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteInstructor(int id)
    {
        var instructor = await _dbContext.Instructors.FindAsync(id);
        if (instructor == null)
        {
            TempData["ErrorMessage"] = "Instructor not found.";
            return RedirectToAction(nameof(InstructorList));
        }

        _dbContext.Instructors.Remove(instructor);
        await _dbContext.SaveChangesAsync();

        TempData["ErrorMessage"] = "Instructor deleted successfully!";
        return RedirectToAction(nameof(InstructorList));
    }
    [HttpGet]
public async Task<IActionResult> InstructorProfile(int id)
{
    var instructor = await _dbContext.Instructors
        .Include(i => i.User)
        .FirstOrDefaultAsync(i => i.id == id);

    if (instructor == null) return NotFound();

    return View(instructor);
}

    [HttpGet]
    public IActionResult Profile()
    {
        var username = User.Identity?.Name;
        var admin = _dbContext.Users.FirstOrDefault(u => u.Username == username);
        if (admin == null) return NotFound();

        return View(admin);
    }
}