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
using Microsoft.Extensions.Logging;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext dbContext, IWebHostEnvironment env, ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _env = env;
        _logger = logger;
    }






    // ===== New: Create Student (Admin only) =====
    [HttpGet]
    public IActionResult CreateStudent()
    {
        // No instructor assignment at creation time — students will be unassigned until an instructor creates a course
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
            UserId = user.Id
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
                Message = "Your instructor account has been approved by admin. An administrator must grant permission before you can create courses.",
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

    // POST: Grant course creation permission to an approved instructor
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantCourseCreation(int id)
    {
        var instructor = await _dbContext.Instructors.FirstOrDefaultAsync(i => i.id == id);
        if (instructor == null) return NotFound();

        if (!instructor.IsApproved)
        {
            TempData["ErrorMessage"] = "Instructor must be approved before granting course creation permission.";
            return RedirectToAction("InstructorList");
        }

        instructor.IsCourseCreationAllowed = true;
        await _dbContext.SaveChangesAsync();

        // Log activity
        var adminUsername = User.Identity?.Name;
        var adminUser = _dbContext.Users.FirstOrDefault(u => u.Username == adminUsername);
        _dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = adminUser?.Id.ToString(),
            ActivityType = ActivityType.GrantCourseCreation,
            Timestamp = DateTimeOffset.UtcNow,
            ResourceId = instructor.id.ToString()
        });
        await _dbContext.SaveChangesAsync();

        // Notify instructor if user exists
        if (instructor.UserId > 0)
        {
            var notification = new Notification
            {
                UserId = instructor.UserId,
                Title = "Course Creation Permission Granted",
                Message = "An administrator has granted you permission to create courses.",
                NotificationType = "grant_course_creation",
                RelatedId = instructor.id,
                IconClass = "fas fa-unlock",
                ActionUrl = "/Instructor/CreateCourse",
                CreatedAt = DateTime.Now,
                IsRead = false
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Course creation permission granted.";
        return RedirectToAction("InstructorList");
    }

    // POST: Revoke course creation permission
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeCourseCreation(int id)
    {
        var instructor = await _dbContext.Instructors.FirstOrDefaultAsync(i => i.id == id);
        if (instructor == null) return NotFound();

        instructor.IsCourseCreationAllowed = false;
        await _dbContext.SaveChangesAsync();

        // Log activity
        var adminUsername = User.Identity?.Name;
        var adminUser = _dbContext.Users.FirstOrDefault(u => u.Username == adminUsername);
        _dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = adminUser?.Id.ToString(),
            ActivityType = ActivityType.ApproveInstructor, // reuse an existing type for audit, or add a new type if desired
            Timestamp = DateTimeOffset.UtcNow,
            ResourceId = instructor.id.ToString()
        });
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Course creation permission revoked.";
        return RedirectToAction("InstructorList");
    }

    [HttpGet]
    public IActionResult StudentList(StudentFilterViewModel filter)
    {
        var query = _dbContext.Students
            .Include(s => s.User)
            .Include(s => s.Instructor)
            .AsQueryable();

        // Search by name (FirstName, MiddleName, LastName)
        if (!string.IsNullOrEmpty(filter?.SearchQuery))
        {
            var searchQuery = filter.SearchQuery.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(searchQuery) ||
                (s.MiddleName ?? "").ToLower().Contains(searchQuery) ||
                s.LastName.ToLower().Contains(searchQuery));
        }

        // Filter by email/username
        if (!string.IsNullOrEmpty(filter?.Email))
        {
            query = query.Where(s => s.User != null && s.User.Username.ToLower().Contains(filter.Email.ToLower()));
        }

        // Filter by grade
        if (!string.IsNullOrEmpty(filter?.Grade))
        {
            query = query.Where(s => s.Grade == filter.Grade);
        }

        // Filter by guardian name
        if (!string.IsNullOrEmpty(filter?.GuardianName))
        {
            query = query.Where(s => s.GuardianName.ToLower().Contains(filter.GuardianName.ToLower()));
        }

        var students = query.OrderBy(s => s.FirstName).ToList();

        var viewModel = new StudentFilterViewModel
        {
            SearchQuery = filter?.SearchQuery,
            Email = filter?.Email,
            Grade = filter?.Grade,
            GuardianName = filter?.GuardianName,
            Students = students
        };

        // Diagnostic log: number of students returned
        _logger?.LogInformation("StudentList returned {Count} students", students.Count);

        return View(viewModel);
    }

    // GET: Admin Edit Student
    [HttpGet]
    public async Task<IActionResult> EditStudent(int id)
    {
        var student = await _dbContext.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();

        var model = new StudentViewModel
        {
            FirstName = student.FirstName,
            MiddleName = student.MiddleName,
            LastName = student.LastName,
            Email = student.Email,
            PhoneNumber = student.PhoneNumber,
            Gender = student.Gender,
            DateOfBirth = student.DateOfBirth,
            Address = student.Address,
            GuardianName = student.GuardianName,
            GuardianPhone = student.GuardianPhone,

            EnrollmentDate = student.EnrollmentDate,
            Grade = student.Grade,
            InstructorId = student.Instructorid,
            Username = student.User?.Username ?? string.Empty
        };

        ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName", student.Instructorid);
        return View(model);
    }

    // POST: Admin Edit Student
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStudent(int id, StudentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var student = await _dbContext.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();

        // Update user info
        if (student.User != null)
        {
            student.User.Username = model.Username;
            if (!string.IsNullOrEmpty(model.Password))
            {
                student.User.Password = model.Password; // hash in prod
            }
        }

        student.FirstName = model.FirstName;
        student.MiddleName = model.MiddleName;
        student.LastName = model.LastName;
        student.Email = model.Email;
        student.PhoneNumber = model.PhoneNumber;
        student.Gender = model.Gender;
        student.DateOfBirth = model.DateOfBirth;
        student.Address = model.Address;
        student.GuardianName = model.GuardianName;
        student.GuardianPhone = model.GuardianPhone;
        student.EnrollmentDate = model.EnrollmentDate;
        student.Grade = model.Grade;
        student.Instructorid = model.InstructorId;

        // Handle profile image update
        if (model.ProfileImage != null && model.ProfileImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "students");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImage.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfileImage.CopyToAsync(stream);
            }

            student.ProfileImagePath = Path.Combine("uploads", "students", fileName).Replace("\\", "/");
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Student updated successfully!";
        return RedirectToAction("StudentList");
    }

    // GET: Admin student profile (reuse instructor view)
    [HttpGet]
    public async Task<IActionResult> StudentProfile(int id)
    {
        var student = await _dbContext.Students
            .Include(s => s.User)
            .Include(s => s.Instructor)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null) return NotFound();

        // Reuse Instructor's StudentProfile view
        return View("~/Views/Instructor/StudentProfile.cshtml", student);
    }

    // POST: Admin Delete student
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var student = await _dbContext.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Student not found.";
            return RedirectToAction(nameof(StudentList));
        }

        try
        {
            _dbContext.Students.Remove(student);
            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Student deleted successfully!";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            TempData["ErrorMessage"] = "Failed to delete student due to related data. Please remove related enrollments/submissions first.";
        }

        return RedirectToAction(nameof(StudentList));
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

        // Students last activity (include last course id for quick lookup)
        var studentLastActivity = _dbContext.Students
            .Include(s => s.User)
            .Select(s => new {
                s.Id,
                FullName = (s.FirstName + " " + s.LastName),
                Username = s.User != null ? s.User.Username : null,
                LastActivity = _dbContext.ActivityLogs
                    .Where(al => s.User != null && al.UserId == s.User.Id.ToString())
                    .OrderByDescending(al => al.Timestamp)
                    .Select(al => al.Timestamp).FirstOrDefault(),
                LastCourseId = _dbContext.ActivityLogs
                    .Where(al => s.User != null && al.UserId == s.User.Id.ToString() && al.CourseId != null)
                    .OrderByDescending(al => al.Timestamp)
                    .Select(al => al.CourseId).FirstOrDefault()
            })
            .OrderByDescending(x => x.LastActivity)
            .ToList();

        // Map course ids to names
        var courseIds = studentLastActivity
            .Where(x => !string.IsNullOrEmpty(x.LastCourseId))
            .Select(x => { int.TryParse(x.LastCourseId, out var id); return id; })
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var courseMap = _dbContext.Courses
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionary(c => c.Id.ToString(), c => c.FullName);

        var studentLastActivityWithCourse = studentLastActivity
            .Select(x => new {
                x.Id,
                x.FullName,
                x.Username,
                x.LastActivity,
                CourseName = (!string.IsNullOrEmpty(x.LastCourseId) && courseMap.ContainsKey(x.LastCourseId)) ? courseMap[x.LastCourseId] : null
            })
            .ToList();

        ViewBag.InstructorCount = instructorCount;
        ViewBag.StudentCount = studentCount;
        ViewBag.ActiveInstructors = activeInstructors;
        ViewBag.ActiveStudents = activeStudents;
        ViewBag.InstructorLastActivity = instructorLastActivity;
        ViewBag.StudentLastActivity = studentLastActivityWithCourse;

        return View();
    }

    // ===== Admin Chart Endpoints (moved from Instructor) =====
    [HttpGet]
    public async Task<IActionResult> GetActivityChartData()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var data = await _dbContext.ActivityLogs
            .Where(l => l.Timestamp >= cutoff)
            .GroupBy(l => l.ActivityType)
            .Select(g => new { label = g.Key.ToString(), count = g.Count() })
            .ToListAsync();
        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetStudentActivityDistribution(int? courseId)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        // Determine student user IDs to include
        List<string> studentUserIds;
        if (courseId.HasValue)
        {
            studentUserIds = await _dbContext.Enrollments
                .Where(e => e.CourseId == courseId.Value)
                .Select(e => e.Student!.UserId.ToString())
                .Distinct()
                .ToListAsync();
        }
        else
        {
            studentUserIds = await _dbContext.Students
                .Where(s => s.UserId != null)
                .Select(s => s.UserId.ToString())
                .Distinct()
                .ToListAsync();
        }

        if (!studentUserIds.Any()) return Json(new List<object>());

        var logs = await _dbContext.ActivityLogs
            .Where(l => l.Timestamp >= cutoff && l.UserId != null && studentUserIds.Contains(l.UserId))
            .ToListAsync();

        var students = await _dbContext.Students
            .Where(s => s.UserId != null)
            .Include(s => s.User)
            .ToListAsync();

        var studentDict = students
            .Where(s => s.User != null)
            .ToDictionary(s => s.User!.Id.ToString(), s => (s.FirstName + " " + s.LastName).Trim());

        var data = logs
            .Where(l => l.UserId != null && studentDict.ContainsKey(l.UserId))
            .GroupBy(l => studentDict[l.UserId!])
            .Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToList();

        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetCourseEnrollmentData()
    {
        var data = await _dbContext.Courses
            .Select(c => new
            {
                label = c.FullName,
                count = c.Enrollments != null ? c.Enrollments.Count() : 0
            })
            .ToListAsync();

        return Json(data);
    }

   

    // GET: Show Create Instructor form
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

        // Admin-created instructors are approved immediately
        instructor.IsApproved = true;
        instructor.ApprovedAt = DateTime.UtcNow;
        var adminUsername = User.Identity?.Name;
        var adminUser = _dbContext.Users.FirstOrDefault(u => u.Username == adminUsername);
        instructor.ApprovedByAdminId = adminUser?.Id;

        _dbContext.Instructors.Add(instructor);
        await _dbContext.SaveChangesAsync();

        // Notify instructor
        var notification = new Notification
        {
            UserId = user.Id,
            Title = "Instructor Account Created",
            Message = $"Your instructor account has been created and approved. You may now create courses!",
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

        // Diagnostic: if you need a quick JSON endpoint to verify data, call /Admin/Diagnostics

        // Search by name (FirstName, MiddleName, LastName)
        if (!string.IsNullOrEmpty(filter?.SearchQuery))
        {
            var searchQuery = filter.SearchQuery.ToLower();
            query = query.Where(i =>
                i.FirstName.ToLower().Contains(searchQuery) ||
                (i.MiddleName ?? "").ToLower().Contains(searchQuery) ||
                i.LastName.ToLower().Contains(searchQuery));
        }

        // Filter by email
        if (!string.IsNullOrEmpty(filter?.Email))
        {
            query = query.Where(i => i.Email.ToLower().Contains(filter.Email.ToLower()));
        }

        // Filter by phone number
        if (!string.IsNullOrEmpty(filter?.PhoneNumber))
        {
            query = query.Where(i => i.PhoneNumber.Contains(filter.PhoneNumber));
        }

        // Filter by qualification
        if (!string.IsNullOrEmpty(filter?.Qualification))
        {
            query = query.Where(i => i.Qualification.ToLower().Contains(filter.Qualification.ToLower()));
        }

        var instructors = query.OrderBy(i => i.FirstName).ToList();

        var viewModel = new InstructorFilterViewModel
        {
            SearchQuery = filter?.SearchQuery,
            Email = filter?.Email,
            PhoneNumber = filter?.PhoneNumber,
            Qualification = filter?.Qualification,
            Instructors = instructors
        };

        // Diagnostic log: number of instructors returned
        _logger?.LogInformation("InstructorList returned {Count} instructors", instructors.Count);

        return View(viewModel);
    }

    // Diagnostics endpoint for quick checks
    [HttpGet]
    public IActionResult Diagnostics()
    {
        var instructorCount = _dbContext.Instructors.Count();
        var studentCount = _dbContext.Students.Count();
        var userCount = _dbContext.Users.Count();
        return Json(new { Instructors = instructorCount, Students = studentCount, Users = userCount });
    }

    // GET: Student activity detail
    [HttpGet]
    public async Task<IActionResult> StudentActivity(int id)
    {
        var student = await _dbContext.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();

        var userId = student.UserId.ToString();

        var logs = await _dbContext.ActivityLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToListAsync();

        // Collect course ids
        var courseIds = logs
            .Where(l => !string.IsNullOrEmpty(l.CourseId))
            .Select(l => { int.TryParse(l.CourseId, out var idVal); return idVal; })
            .Where(i => i > 0)
            .Distinct()
            .ToList();

        var courses = await _dbContext.Courses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id.ToString(), c => c.FullName);

        var displayLogs = logs.Select(l => new ActivityLogDisplayItem
        {
            Id = l.Id,
            ActivityType = l.ActivityType,
            CourseId = l.CourseId,
            CourseName = (!string.IsNullOrEmpty(l.CourseId) && courses.ContainsKey(l.CourseId)) ? courses[l.CourseId] : null,
            DurationSeconds = l.DurationSeconds,
            IpAddress = l.IpAddress,
            MetadataJson = l.MetadataJson,
            ResourceId = l.ResourceId,
            ResourceTitle = l.ResourceId, // could be improved
            Timestamp = l.Timestamp,
            UserAgent = l.UserAgent,
            UserId = l.UserId,
            StudentName = (student.FirstName + " " + student.LastName).Trim()
        }).ToList();

        var viewModel = new ActivityLogFilterViewModel
        {
            StudentId = student.User?.Username ?? student.UserId.ToString(),
            FromDate = null,
            ToDate = null,
            ActivityType = null,
            Logs = displayLogs
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

        // Check for dependent data that would block deletion
        var courseCount = await _dbContext.Courses.CountAsync(c => c.Instructorid == id);
        if (courseCount > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete instructor. They have {courseCount} course(s). Reassign or delete those courses first.";
            return RedirectToAction(nameof(InstructorList));
        }

        try
        {
            _dbContext.Instructors.Remove(instructor);
            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Instructor deleted successfully!";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting instructor {InstructorId}", id);
            TempData["ErrorMessage"] = "Failed to delete instructor due to related data. Please remove related courses/assignments/materials first.";
        }

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