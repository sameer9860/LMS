using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using System.Threading.Tasks;
using LMS.Views.Data;
using LMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Text.RegularExpressions;
// Add the following using if CourseViewModel is in LMS.ViewModels
using LMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
// Ensure this matches the namespace where StudentViewModel is defined
// Ensure this matches the namespace where CourseViewModel is


// Ensure this matches the namespace where CourseViewModel will be created

public class InstructorController : Controller
{
    private readonly ApplicationDbContext _dbContext;


    private readonly IWebHostEnvironment _env;

    private readonly ILogger<InstructorController> _logger;
    public InstructorController(ApplicationDbContext dbContext, IWebHostEnvironment env, ILogger<InstructorController> logger)
    {

        _dbContext = dbContext;
        _env = env;
        _logger = logger;
        //_notificationService = notificationService;
    }

    public IActionResult Dashboard()
    {
        var username = User.Identity?.Name;
        var instructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User!.Username == username);

        if (instructor == null)
            return RedirectToAction("Login", "Account");

        // Total courses for this instructor
        var totalCourses = _dbContext.Courses.Count(c => c.Instructorid == instructor.id);

        // Total enrolled students in all courses of this instructor
        var totalEnrolledStudents = _dbContext.Enrollments
            .Where(e => e.Course!.Instructorid == instructor.id)
            .Select(e => e.StudentId)
            .Distinct()
            .Count();

        ViewBag.TotalCourses = totalCourses;
        ViewBag.TotalEnrolledStudents = totalEnrolledStudents;

        return View();
    }

    // GET: /Instructor/Dashboard
    public IActionResult Course()
    {
        // Get username from session
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            TempData["Err"] = "Unauthorized access.";
            return RedirectToAction("Login", "Account");
        }

        // Find instructor by username
        var instructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User != null && i.User.Username == username);

        if (instructor == null)
        {
            TempData["ErrorMessage"] = "Instructor not found.";
            return RedirectToAction("Login", "Account");
        }

        // Get courses for this instructor
        var courses = _dbContext.Courses
            .Where(c => c.Instructorid == instructor.id)
            .ToList();

        // Render the Dashboard view in Views/Instructor/Dashboard.cshtml
        return View("Course", courses);
    }
    // GET: List students
    [HttpGet]
    public IActionResult StudentList()
    {
        // Get the logged-in instructor's username
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            TempData["Error"] = "Unauthorized access.";
            return RedirectToAction("Login", "Account");
        }

        // Find the instructor by username
        var instructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User != null && i.User.Username == username);

        if (instructor == null)
        {
            TempData["ErrorMessage"] = "Instructor not found.";
            return RedirectToAction("Login", "Account");
        }

        // Only get students assigned to this instructor
        var students = _dbContext.Students
            .Include(s => s.User)
            .Where(s => s.Instructorid == instructor.id)
            .ToList();

        return View(students);
    }

    // GET: Show form to create student
    [HttpGet]
    public IActionResult EnrollStudents()
    {
        ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName");
        return View(new StudentViewModel());
    }

    // POST: Handle create student
    [HttpPost]
    public async Task<IActionResult> EnrollStudents(StudentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName");
            return View(model);
        }

        try
        {
            var user = new User
            {
                Username = model.Username,
                Password = model.Password,
                Role = "Student"
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            string imagePath = string.Empty;
            if (model.ProfileImage != null)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads", "students");
                Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImage.FileName);
                var filePath = Path.Combine(uploads, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
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
                Address = model.Address,
                GuardianName = model.GuardianName,
                GuardianPhone = model.GuardianPhone,
                Grade = model.Grade,
                EnrollmentDate = model.EnrollmentDate,
                ProfileImagePath = imagePath,
                UserId = user.Id,
                Instructorid = model.InstructorId
            };

            _dbContext.Students.Add(student);
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student created successfully!";
            return RedirectToAction("StudentList");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error: " + ex.Message);
            ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName");
            return View(model);
        }
    }

    // GET: Edit student
    [HttpGet]
    public async Task<IActionResult> EditStudent(int id)
    {
        var student = await _dbContext.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

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
            Grade = student.Grade,
            EnrollmentDate = student.EnrollmentDate,
            InstructorId = student.Instructorid,
            Username = student.User?.Username ?? string.Empty,
            // Do not pre-fill password fields for security
        };

        ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName", student.Instructorid);
        return View(model);
    }

    // POST: Handle edit
    [HttpPost]
    public async Task<IActionResult> EditStudent(int id, StudentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName", model.InstructorId);
            return View(model);
        }

        var student = await _dbContext.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();

        // Update user info (username, password if changed)
        if (student.User != null)
        {
            student.User.Username = model.Username;
            if (!string.IsNullOrEmpty(model.Password))
            {
                student.User.Password = model.Password; // hash in prod
            }
        }

        // Update student fields
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
        student.Grade = model.Grade;
        student.EnrollmentDate = model.EnrollmentDate;
        student.Instructorid = model.InstructorId;

        // Handle profile image update
        if (model.ProfileImage != null)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads", "students");
            Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImage.FileName);
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.ProfileImage.CopyToAsync(stream);
            student.ProfileImagePath = Path.Combine("uploads", "students", fileName).Replace("\\", "/");
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Student updated successfully!";
        return RedirectToAction("StudentList");
    }

    // POST: Delete student
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

        _dbContext.Students.Remove(student);
        await _dbContext.SaveChangesAsync();

        TempData["ErrorMessage"] = "Student deleted successfully!";
        return RedirectToAction(nameof(StudentList));
    }

    // GET: Student profile (show details)
    [HttpGet]
    public async Task<IActionResult> StudentProfile(int id)
    {
        var student = await _dbContext.Students
            .Include(s => s.User)
            .Include(s => s.Instructor)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null) return NotFound();

        return View(student);
    }
    [HttpGet]
    public IActionResult CreateCourse()
    {
        ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(CourseViewModels model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName");
            return View(model);
        }

        // Handle image upload
        string imagePath = string.Empty;
        if (model.CourseImage != null)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "courses");
            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(model.CourseImage.FileName);
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.CourseImage.CopyToAsync(stream);
            }

            imagePath = Path.Combine("uploads", "courses", fileName).Replace("\\", "/");
        }

        // Create course with selected instructor
        var course = new Course
        {
            FullName = model.FullName,
            ShortName = model.ShortName,
            Category = model.Category,
            IsVisible = model.IsVisible,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            CourseIdNumber = model.CourseIdNumber,
            Summary = model.Summary,
            ImagePath = imagePath,
            Instructorid = model.InstructorId // <-- Use selected instructor from form
        };

        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessCourse"] = "Course created successfully!";
        return RedirectToAction("Course");
    }

    public IActionResult CourseDetails(int id)
    {
        if (id <= 0) return NotFound();
        var course = _dbContext.Courses
            .Include(c => c.Materials)
            .Include(c => c.Assignments)
            .Include(c => c.Enrollments!)
            .ThenInclude(e => e.Student)
            .Include(c => c.Quizzes) // Include quizzes

            .Include(c => c.LiveClasses)
            // .Include(c => c.Instructor) // Make sure to include instructors if needed
            .FirstOrDefault(c => c.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        // Get the logged-in instructor's ID
        var username = User.Identity?.Name;
        var instructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User!.Username == username);

        // Get IDs of students already enrolled in this course
        var enrolledStudentIds = course.Enrollments!.Select(e => e.StudentId).ToList();

        // Get students for this instructor who are NOT already enrolled
        var instructorStudents = _dbContext.Students
            .Where(s => s.Instructorid == instructor!.id && !enrolledStudentIds.Contains(s.Id))
            .ToList();

        // Get chat messages sorted by time
        var chatMessages = _dbContext.ChatMessages
          .Include(m => m.User)
          .Where(m => m.CourseId == id)
          .OrderBy(m => m.SentAt)
          .ToList();

        ViewBag.ChatMessages = chatMessages;

        ViewBag.instructorStudents = instructorStudents;

        // Set default times for the new live class (e.g., now + 1 hour to now + 2 hours)
        var defaultStartTime = DateTime.Now;
        var defaultEndTime = defaultStartTime.AddHours(1);

        var viewModel = new CourseDetailsViewModel
        {
            Id = course.Id,
            Course = course,
            CourseId = course.Id,
            Students = course.Enrollments?
                .Where(e => e.Student != null)
                .Select(e => e.Student!)
                .ToList(),

            Materials = course.Materials?.ToList(),
            Assignments = course.Assignments?.ToList(),
            Enrollments = course.Enrollments?.ToList(),
            LiveClasses = course.LiveClasses?.OrderBy(lc => lc.StartTime).ToList(),


            NewLiveClass = new LiveClassViewModel
            {
                CourseId = course.Id,
                StartTime = defaultStartTime,
                EndTime = defaultEndTime,
                // Set default room name or leave it to be generated in the controller
                RoomName = $"class-{course.Id}-{DateTime.Now.Ticks}"
            }
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> EnrollStudentToCourse(int courseId, int studentId)
    {
        var exists = _dbContext.Enrollments.Any(e => e.CourseId == courseId && e.StudentId == studentId);

        if (!exists)
        {
            var enrollment = new Enrollment
            {
                CourseId = courseId,
                StudentId = studentId
            };
            _dbContext.Enrollments.Add(enrollment);
            await _dbContext.SaveChangesAsync();
            TempData["SuccessEnroll"] = "Student enrolled successfully!";
        }
        else
        {
            TempData["ErrorMessageEnroll"] = "Student is already enrolled in this course.";
        }

        return RedirectToAction("CourseDetails", new { id = courseId });
    }

    [HttpGet]
    public IActionResult EditCourse(int id)
    {
        var username = User.Identity?.Name;

        var instructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User!.Username == username);

        if (instructor == null)
        {
            TempData["ErrorMessage"] = "Unauthorized access.";
            return RedirectToAction("Dashboard");
        }

        var course = _dbContext.Courses
            .FirstOrDefault(c => c.Id == id && c.Instructorid == id);

        if (course == null)
        {
            TempData["ErrorMessage"] = "Course not found.";
            return RedirectToAction("Dashboard");
        }

        var viewModel = new CourseDetailsViewModel
        {
            Course = course,
            Id = course.Id
        };

        return View("CourseDetails", viewModel); // assuming you render edit form inside CourseDetails tab
    }


    [HttpPost]
    public IActionResult EditCourse(int Id, IFormFile ImagePath, string FullName, string ShortName, string Category, string CourseIdNumber, DateTime StartDate, DateTime EndDate, string Summary)
    {
        var course = _dbContext.Courses.FirstOrDefault(c => c.Id == Id);

        if (course == null)
        {
            TempData["ErrorMessage"] = "Course not found.";
            return RedirectToAction("Dashboard");
        }

        // Update course properties
        course.FullName = FullName;
        course.ShortName = ShortName;
        course.Category = Category;
        course.CourseIdNumber = CourseIdNumber;
        course.StartDate = StartDate;
        course.EndDate = EndDate;
        course.Summary = Summary;

        // Save image if uploaded
        if (ImagePath != null && ImagePath.Length > 0)
        {
            var fileName = Path.GetFileName(ImagePath.FileName);
            var imagePath = Path.Combine("wwwroot/images/courses", fileName);

            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                ImagePath.CopyTo(stream);
            }

            course.ImagePath = "images/courses/" + fileName;
        }

        _dbContext.SaveChanges();

        TempData["SuccessEditCourse"] = "Course updated successfully!";
        return RedirectToAction("CourseDetails", new { id = Id });
    }







    [HttpGet]
    public IActionResult AddMaterial(int courseId)
    {
        var model = new MaterialViewModel { CourseId = courseId };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddMaterial(MaterialViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var folder = Path.Combine(_env.WebRootPath, "uploads", "materials");
        Directory.CreateDirectory(folder);

        var fileName = Guid.NewGuid() + Path.GetExtension(model.File!.FileName);
        var fullPath = Path.Combine(folder, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await model.File.CopyToAsync(stream);
        }

        string relativePath = Path.Combine("uploads", "materials", fileName).Replace("\\", "/");
        string fileType = model.File.ContentType;

        var material = new Material
        {
            CourseId = model.CourseId,
            Title = model.Title,
            Description = model.Description,
            FilePath = relativePath,
            FileType = fileType,
            UploadDate = DateTime.Now
        };

        _dbContext.Materials.Add(material);
        await _dbContext.SaveChangesAsync();

        // ✅ Send notifications to enrolled students
        var enrolledStudentIds = _dbContext.Enrollments
            .Where(e => e.CourseId == model.CourseId)
            .Select(e => e.Student!.UserId) // Assuming Student has navigation to User
            .ToList();

        foreach (var userId in enrolledStudentIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = "New Material Uploaded",
                Message = $"New material \"{model.Title}\" has been added to your course.",
                NotificationType = "material",
                RelatedId = material.Id,
                CreatedAt = DateTime.Now,
                IconClass = "bi bi-file-earmark-text",
                ActionUrl = $"/Student/CourseDetails/{model.CourseId}?tab=materials"
            };

            _dbContext.Notifications.Add(notification);
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMaterials"] = "Material uploaded successfully and students notified!";
        return RedirectToAction("CourseDetails", new { id = model.CourseId });
    }

    [HttpGet]
    public IActionResult GiveAssignment(int courseId)
    {
        var model = new AssignmentViewModel
        {
            CourseId = courseId,
            AssignedDate = DateTime.Now, // default to today
            DueDate = DateTime.Today.AddDays(7) // default 1 week later
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GiveAssignment(AssignmentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var assignment = new Assignment
        {
            CourseId = model.CourseId,
            Title = model.Title,
            Description = model.Description,
            FilePath = "", // optional
            PossiblePoints = model.PossiblePoints,
            PassMarks = model.PassMarks,
            AssignedDate = DateTime.Now,
            DueDate = model.DueDate,
            Materials = new List<Material>()
        };

        if (model.File != null && model.File.Length > 0)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "assignments");
            Directory.CreateDirectory(folder); // ensure folder exists

            var fileName = Guid.NewGuid() + Path.GetExtension(model.File.FileName);
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("uploads", "assignments", fileName).Replace("\\", "/");

            assignment.Materials.Add(new Material
            {
                Title = model.File.FileName,
                Description = "Assignment Attachment",
                FilePath = relativePath,
                FileType = model.File.ContentType,
                UploadDate = DateTime.Now,
                CourseId = model.CourseId
            });
        }

        _dbContext.Assignments.Add(assignment);
        await _dbContext.SaveChangesAsync();

        // Notify enrolled students
        var enrolledUsers = _dbContext.Enrollments
            .Include(e => e.Student)
            .ThenInclude(s => s!.User)
            .Where(e => e.CourseId == model.CourseId)
            .Select(e => e.Student!.User)
            .ToList();

        foreach (var user in enrolledUsers)
        {
            _dbContext.Notifications.Add(new Notification
            {
                UserId = user!.Id,
                Title = "New Assignment",
                Message = $"A new assignment \"{assignment.Title}\" has been added.",
                NotificationType = "assignment",
                RelatedId = assignment.Id,
                IconClass = "fas fa-tasks",
                ActionUrl = $"/Student/CourseDetails/{model.CourseId}?tab=assignments",
                CreatedAt = DateTime.Now
            });
        }

        await _dbContext.SaveChangesAsync();

        // ✅ Set success message and redirect like materials
        TempData["SuccessAssignment"] = "Assignment added successfully!";
        return RedirectToAction("CourseDetails", new { id = model.CourseId });
    }


    [HttpGet]
    public IActionResult ViewSubmissions(int assignmentId)
    {
        var assignment = _dbContext.Assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (assignment == null)
            return NotFound();

        var submissions = _dbContext.AssignmentSubmissions
            .Include(s => s.Student)
            .Include(s => s.Assignment)
            .Where(s => s.AssignmentId == assignmentId)
            .ToList();

        ViewBag.AssignmentTitle = assignment.Title;
        return View(submissions);
    }

    [HttpGet]
    public IActionResult GradeSubmission(int id)
    {
        var submission = _dbContext.AssignmentSubmissions
            .Include(s => s.Student)
            .Include(s => s.Assignment)
            .FirstOrDefault(s => s.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        return View(submission);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GradeSubmission(AssignmentSubmission model)
    {
        var submission = _dbContext.AssignmentSubmissions
            .FirstOrDefault(s => s.Id == model.Id);

        if (submission == null)
        {
            return NotFound();
        }

        submission.MarksObtained = model.MarksObtained;
        submission.IsPassed = model.IsPassed;
        submission.Feedback = model.Feedback;

        await _dbContext.SaveChangesAsync();

        TempData["SuccessGrade"] = "Grading saved successfully!";
        return RedirectToAction("ViewSubmissions", new { assignmentId = submission.AssignmentId });
    }





    // POST: Save the scheduled live class
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLiveClass(LiveClassViewModel model)
    {
        if (model.CourseId <= 0)
        {
            TempData["ErrorLiveClass"] = "Invalid course ID.";
            return RedirectToAction("CourseDetails", new { id = model.CourseId, tab = "live-classes" });
        }

        // If ModelState is invalid, reload the CourseDetails view with errors
        if (!ModelState.IsValid)
        {
            var course = _dbContext.Courses
                .Include(c => c.Materials)
                .Include(c => c.Assignments)
                    .Include(c => c.Enrollments!)
                    .ThenInclude(e => e.Student)
                .Include(c => c.LiveClasses)
                .FirstOrDefault(c => c.Id == model.CourseId);

            if (course == null)
            {
                TempData["ErrorLiveClass"] = "Course not found.";
                return NotFound();
            }

            var username = User.Identity?.Name;
            var instructor = _dbContext.Instructors
                .Include(i => i.User)
                .FirstOrDefault(i => i.User!.Username == username);

            var enrolledStudentIds = course.Enrollments!.Select(e => e.StudentId).ToList();
            var instructorStudents = _dbContext.Students
                .Where(s => s.Instructorid == instructor!.id && !enrolledStudentIds.Contains(s.Id))
                .ToList();
            ViewBag.instructorStudents = instructorStudents;

            var viewModel = new CourseDetailsViewModel
            {
                Id = course.Id,
                Course = course,
                CourseId = course.Id,
                Students = course.Enrollments?
                    .Where(e => e.Student != null)
                    .Select(e => e.Student!)
                    .ToList(),
                Materials = course.Materials?.ToList(),
                Assignments = course.Assignments?.ToList(),
                Enrollments = course.Enrollments?.ToList(),
                LiveClasses = course.LiveClasses?.OrderBy(lc => lc.StartTime).ToList(),
                NewLiveClass = model
            };

            return View("CourseDetails", viewModel);
        }

        if (model.StartTime < DateTime.Now)
        {
            TempData["ErrorLiveClass"] = "Start time cannot be in the past.";
            return RedirectToAction("CourseDetails", new { id = model.CourseId, tab = "live-classes" });
        }

        if (model.EndTime <= model.StartTime)
        {
            TempData["ErrorLiveClass"] = "End time must be after start time.";
            return RedirectToAction("CourseDetails", new { id = model.CourseId, tab = "live-classes" });
        }

        try
        {
            var courseExists = await _dbContext.Courses.AnyAsync(c => c.Id == model.CourseId);
            if (!courseExists)
            {
                TempData["ErrorLiveClass"] = "Course not found.";
                return NotFound();
            }

            var newLive = new LiveClass
            {
                CourseId = model.CourseId,
                Title = model.Title?.Trim(),
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Description = model.Description?.Trim(),
                IsLive = false,
                IsCompleted = false,
                RoomName = "class-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            };

            _dbContext.LiveClasses.Add(newLive);
            await _dbContext.SaveChangesAsync();

            // ✅ Send notifications to enrolled students
            var enrolledStudentIds = _dbContext.Enrollments
                .Where(e => e.CourseId == model.CourseId)
                .Select(e => e.Student!.UserId)
                .ToList();

            foreach (var userId in enrolledStudentIds)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = "New Live Class Scheduled",
                    Message = $"A new live class \"{model.Title}\" has been scheduled.",
                    NotificationType = "live-class",
                    RelatedId = newLive.Id,
                    CreatedAt = DateTime.Now,
                    IconClass = "bi bi-camera-video",
                    ActionUrl = $"/Student/CourseDetails/{model.CourseId}?tab=live-classes"
                };

                _dbContext.Notifications.Add(notification);
            }

            await _dbContext.SaveChangesAsync();

            TempData["SuccessLiveClass"] = $"Live class '{model.Title}' scheduled successfully!";
            return RedirectToAction("CourseDetails", new { id = model.CourseId, tab = "live-classes" });
        }
        catch (Exception ex)
        {
            TempData["ErrorLiveClass"] = "An error occurred while scheduling the live class." + ex.Message;
            return RedirectToAction("CourseDetails", new { id = model.CourseId, tab = "live-classes" });
        }
    }

    // Helper method for better room name generation

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteLiveClass(int id)
    {
        var liveClass = _dbContext.LiveClasses.Find(id);
        if (liveClass == null)
            return NotFound();

        int courseId = liveClass.CourseId;

        _dbContext.LiveClasses.Remove(liveClass);
        _dbContext.SaveChanges();

        TempData["SuccessDeleteClass"] = "Live class deleted successfully.";
        return RedirectToAction("CourseDetails", new { id = courseId, tab = "live-classes" });
    }



    public IActionResult JoinLiveClass(int id)
    {
        var liveClass = _dbContext.LiveClasses
            .Include(l => l.Course)
            .FirstOrDefault(l => l.Id == id);

        if (liveClass == null) return NotFound();

        // Redirect or view for live session
        return View("JoinLiveClass", liveClass); // You can update this view to launch the meeting.
    }

    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> ActivityLogs(ActivityLogFilterViewModel filter)
    {
        var query = _dbContext.ActivityLogs.AsQueryable();

        // Filter by student
        if (!string.IsNullOrEmpty(filter.StudentId))
            query = query.Where(x => x.UserId == filter.StudentId);

        // Filter by course
        if (!string.IsNullOrEmpty(filter.CourseId))
            query = query.Where(x => x.CourseId == filter.CourseId);

        // Filter by activity type
        if (filter.ActivityType.HasValue)
            query = query.Where(x => x.ActivityType == filter.ActivityType);

        // Filter by date range
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.Timestamp >= filter.FromDate);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.Timestamp <= filter.ToDate.Value.AddDays(1));

        // Order newest first
        var logs = await query
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();

        // Map UserIds (Usernames) to Names
        var appUsernames = logs
            .Where(l => !string.IsNullOrEmpty(l.UserId))
            .Select(l => l.UserId)
            .Distinct()
            .ToList();

        // ActivityLog.UserId stores the Username string.
        // We need to find Students who have a User with that Username.
        var students = await _dbContext.Students
            .Include(s => s.User)
            .Where(s => s.User != null && appUsernames.Contains(s.User.Username))
            .Select(s => new { Username = s.User!.Username, Name = s.FirstName + " " + s.LastName })
            .ToListAsync();

        var studentDict = students.ToDictionary(s => s.Username!, s => s.Name);

        // Map CourseIds to Names
        var courseIds = logs
            .Where(l => !string.IsNullOrEmpty(l.CourseId))
            .Select(l => l.CourseId)
            .Distinct()
            .ToList();
            
        var courseIdsInt = new List<int>();
        foreach (var cid in courseIds)
        {
             if (int.TryParse(cid, out int id)) courseIdsInt.Add(id);
        }

        var courses = await _dbContext.Courses
            .Where(c => courseIdsInt.Contains(c.Id))
            .Select(c => new { c.Id, c.FullName })
            .ToListAsync();

        var courseDict = courses.ToDictionary(c => c.Id.ToString(), c => c.FullName);

        // --- RESOURCE MAPPING ---
        // 1. Materials
        var materialIds = logs
            .Where(l => !string.IsNullOrEmpty(l.ResourceId) && 
                       (l.ActivityType == ActivityType.ViewMaterial || l.ActivityType == ActivityType.DownloadMaterial))
            .Select(l => l.ResourceId)
            .Distinct()
            .ToList();
        var matIdsInt = materialIds.Select(id => int.TryParse(id, out int i) ? i : 0).Where(i => i > 0).ToList();
        var materials = await _dbContext.Materials.Where(m => matIdsInt.Contains(m.Id)).Select(m => new { m.Id, m.Title }).ToListAsync();
        var materialDict = materials.ToDictionary(m => m.Id.ToString(), m => m.Title);

        // 2. Assignments
        var assignmentIds = logs
            .Where(l => !string.IsNullOrEmpty(l.ResourceId) && 
                       (l.ActivityType == ActivityType.StartAssignment || l.ActivityType == ActivityType.SubmitAssignment))
            .Select(l => l.ResourceId)
            .Distinct()
            .ToList();
        var assignIdsInt = assignmentIds.Select(id => int.TryParse(id, out int i) ? i : 0).Where(i => i > 0).ToList();
        var assignments = await _dbContext.Assignments.Where(a => assignIdsInt.Contains(a.Id)).Select(a => new { a.Id, a.Title }).ToListAsync();
        var assignmentDict = assignments.ToDictionary(a => a.Id.ToString(), a => a.Title);

        // 3. Live Classes
        var liveClassIds = logs
             .Where(l => !string.IsNullOrEmpty(l.ResourceId) && 
                        (l.ActivityType == ActivityType.JoinLiveClass || l.ActivityType == ActivityType.LeaveLiveClass))
             .Select(l => l.ResourceId)
             .Distinct()
             .ToList();
        var liveIdsInt = liveClassIds.Select(id => int.TryParse(id, out int i) ? i : 0).Where(i => i > 0).ToList();
        var liveClasses = await _dbContext.LiveClasses.Where(l => liveIdsInt.Contains(l.Id)).Select(l => new { l.Id, l.Title }).ToListAsync();
        var liveClassDict = liveClasses.ToDictionary(l => l.Id.ToString(), l => l.Title);


        // Filter to ONLY students and map data
        filter.Logs = logs
            .Where(l => l.UserId != null && studentDict.ContainsKey(l.UserId)) // Must be in studentDict
            .Select(l => {
                string resourceTitle = "-";
                if (!string.IsNullOrEmpty(l.ResourceId))
                {
                    if (l.ActivityType == ActivityType.ViewMaterial || l.ActivityType == ActivityType.DownloadMaterial)
                        resourceTitle = materialDict.ContainsKey(l.ResourceId) ? materialDict[l.ResourceId] : "Unknown Material";
                    else if (l.ActivityType == ActivityType.StartAssignment || l.ActivityType == ActivityType.SubmitAssignment)
                        resourceTitle = assignmentDict.ContainsKey(l.ResourceId) ? assignmentDict[l.ResourceId] : "Unknown Assignment";
                    else if (l.ActivityType == ActivityType.JoinLiveClass || l.ActivityType == ActivityType.LeaveLiveClass)
                        resourceTitle = liveClassDict.ContainsKey(l.ResourceId) ? liveClassDict[l.ResourceId] : "Unknown Class"; 
                }

                return new ActivityLogDisplayItem
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    CourseId = l.CourseId,
                    ResourceId = l.ResourceId,
                    ActivityType = l.ActivityType,
                    Timestamp = l.Timestamp,
                    DurationSeconds = l.DurationSeconds,
                    IpAddress = l.IpAddress,
                    UserAgent = l.UserAgent,
                    MetadataJson = l.MetadataJson,
                    StudentName = studentDict[l.UserId!],
                    CourseName = (l.CourseId != null && courseDict.ContainsKey(l.CourseId)) 
                                 ? courseDict[l.CourseId] 
                                 : "-",
                    ResourceTitle = resourceTitle
                };
            }).ToList();

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> GetActivityChartData()
    {
        // Get all active student usernames
        var studentUsernames = await _dbContext.Students
            .Include(s => s.User)
            .Where(s => s.User != null && !string.IsNullOrEmpty(s.User.Username))
            .Select(s => s.User!.Username)
            .ToListAsync();
            
        // Get activity stats for the last 30 days ONLY for students (ActivityLog.UserId == Username)
        var data = await _dbContext.ActivityLogs
            .Where(l => l.Timestamp >= DateTimeOffset.UtcNow.AddDays(-30) && 
                        l.UserId != null && 
                        studentUsernames.Contains(l.UserId))
            .GroupBy(l => l.ActivityType)
            .Select(g => new { label = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetStudentActivityDistribution()
    {
        // Get all active students with Usernames
        var students = await _dbContext.Students
            .Include(s => s.User)
            .Where(s => s.User != null && !string.IsNullOrEmpty(s.User.Username))
            .Select(s => new { Username = s.User!.Username, Name = s.FirstName + " " + s.LastName })
            .ToListAsync();
            
        var studentDict = students.ToDictionary(s => s.Username!, s => s.Name);
        var excluded = new[] { "Unknown", "admin" }; // excluded usernames

        var logs = await _dbContext.ActivityLogs
            .Where(l => l.Timestamp >= DateTimeOffset.UtcNow.AddDays(-30) && l.UserId != null)
            .ToListAsync();

        // Group by Student Name
        var data = logs
            .Where(l => studentDict.ContainsKey(l.UserId!) && !excluded.Contains(l.UserId))
            .GroupBy(l => studentDict[l.UserId!])
            .Select(g => new { label = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10) // Top 10 active students
            .ToList();

        return Json(data);
    }


}