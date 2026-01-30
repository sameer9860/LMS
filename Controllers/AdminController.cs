using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using LMS.Views.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Models;
using LMS.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public AdminController(ApplicationDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }






    // ===== New: Create Student (Admin only) =====
    [HttpGet]
    public IActionResult CreateStudent()
    {
        ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent(StudentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Create user
        var user = new User
        {
            Username = model.Username,
            Password = model.Password, // hash in prod
            Role = "Student"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        string imagePath = null;
        if (model.ProfileImage != null)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "students");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Path.GetRandomFileName() + Path.GetExtension(model.ProfileImage.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await model.ProfileImage.CopyToAsync(stream);
            imagePath = Path.Combine("uploads", "students", fileName).Replace("\\", "/");
        }

        var student = new Student
        {
            FirstName = model.FirstName,
            MiddleName = model.MiddleName,
            LastName = model.LastName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            Grade = model.Grade,
            EnrollmentDate = model.EnrollmentDate,
            GuardianName = model.GuardianName,
            GuardianPhone = model.GuardianPhone,
            Address = model.Address,
            ProfileImagePath = imagePath,
            UserId = user.Id,
            Instructorid = model.InstructorId
        };

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync();

        // Activity log
        _dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = user.Id.ToString(),
            ActivityType = ActivityType.CreateStudent,
            Timestamp = DateTimeOffset.UtcNow,
            CourseId = null,
            MetadataJson = null
        });
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Student created successfully!";
        return RedirectToAction("Dashboard");
    }

    // GET: pending instructors
    [HttpGet]
    public IActionResult PendingInstructors()
    {
        var pending = _dbContext.Instructors.Where(i => !i.IsApproved).Include(i => i.User).ToList();
        return View(pending);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveInstructor(int id)
    {
        var instructor = await _dbContext.Instructors.FirstOrDefaultAsync(i => i.id == id);
        if (instructor == null) return NotFound();

        instructor.IsApproved = true;
        instructor.ApprovedAt = DateTime.UtcNow;
        // If you want link to admin user, try to find current admin user id
        var adminUsername = User.Identity?.Name;
        var adminUser = _dbContext.Users.FirstOrDefault(u => u.Username == adminUsername);
        instructor.ApprovedByAdminId = adminUser?.Id;
        await _dbContext.SaveChangesAsync();

        // Log activity
        _dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = adminUser?.Id.ToString(),
            ActivityType = ActivityType.ApproveInstructor,
            Timestamp = DateTimeOffset.UtcNow,
            ResourceId = instructor.id.ToString(),
            MetadataJson = null
        });
        await _dbContext.SaveChangesAsync();

        // Notify instructor if user exists
        if (instructor.UserId > 0)
        {
            var notification = new Notification
            {
                UserId = instructor.UserId,
                Title = "Instructor Approved",
                Message = "Your instructor account has been approved by admin. You may now create courses.",
                NotificationType = "instructor_approved",
                RelatedId = instructor.id,
                IconClass = "fas fa-check",
                ActionUrl = "/Instructor/CreateCourse",
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Instructor approved.";
        return RedirectToAction("PendingInstructors");
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

        // Active = users with any activity in last 30 days
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        int activeInstructors = _dbContext.ActivityLogs
            .Where(a => a.Timestamp >= cutoff)
            .Join(_dbContext.Users, a => a.UserId, u => u.Id.ToString(), (a, u) => u)
            .Count(u => u.Role == "Instructor");

        int activeStudents = _dbContext.ActivityLogs
            .Where(a => a.Timestamp >= cutoff)
            .Join(_dbContext.Users, a => a.UserId, u => u.Id.ToString(), (a, u) => u)
            .Count(u => u.Role == "Student");

        // Instructors last active
        var instructorLastActivity = _dbContext.Instructors
            .Include(i => i.User)
            .Select(i => new {
                i.id,
                i.FirstName,
                i.LastName,
                Username = i.User != null ? i.User.Username : null,
                LastActivity = _dbContext.ActivityLogs
                    .Where(al => i.User != null && al.UserId == i.User.Id.ToString())
                    .OrderByDescending(al => al.Timestamp)
                    .Select(al => al.Timestamp).FirstOrDefault()
            })
            .OrderByDescending(x => x.LastActivity)
            .ToList();

        ViewBag.InstructorCount = instructorCount;
        ViewBag.StudentCount = studentCount;
        ViewBag.ActiveInstructors = activeInstructors;
        ViewBag.ActiveStudents = activeStudents;
        ViewBag.InstructorLastActivity = instructorLastActivity;

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