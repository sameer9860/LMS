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

[Authorize(Roles = "Instructor")]
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
    public IActionResult StudentList(StudentFilterViewModel filter)
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
        var query = _dbContext.Students
            .Include(s => s.User)
            .Where(s => s.Instructorid == instructor.id)
            .AsQueryable();

        // Search by name (FirstName, MiddleName, LastName)
        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            var searchQuery = filter.SearchQuery.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(searchQuery) ||
                s.MiddleName.ToLower().Contains(searchQuery) ||
                s.LastName.ToLower().Contains(searchQuery));
        }

        // Filter by email
        if (!string.IsNullOrEmpty(filter.Email))
        {
            query = query.Where(s => s.User != null && s.User.Username.ToLower().Contains(filter.Email.ToLower()));
        }

        // Filter by grade
        if (!string.IsNullOrEmpty(filter.Grade))
        {
            query = query.Where(s => s.Grade == filter.Grade);
        }

        // Filter by guardian name
        if (!string.IsNullOrEmpty(filter.GuardianName))
        {
            query = query.Where(s => s.GuardianName.ToLower().Contains(filter.GuardianName.ToLower()));
        }

        var students = query.OrderBy(s => s.FirstName).ToList();

        var viewModel = new StudentFilterViewModel
        {
            SearchQuery = filter.SearchQuery,
            Email = filter.Email,
            Grade = filter.Grade,
            GuardianName = filter.GuardianName,
            Students = students
        };

        return View(viewModel);
    }

    // GET: Show page to enroll students (admin handles creation)
    [HttpGet]
    public IActionResult EnrollStudents()
    {
        // Inform instructors that only admin can create student accounts and provide list of existing students
        var username = User.Identity?.Name;
        var loggedInInstructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User!.Username == username);

        var instructorId = loggedInInstructor?.id ?? 0;

        ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName", instructorId);
        // Show students that are enrolled in this instructor's courses (distinct)
        ViewBag.Students = _dbContext.Enrollments
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Include(e => e.Course)
            .Where(e => e.Course != null && e.Course.Instructorid == instructorId)
            .Select(e => e.Student)
            .Distinct()
            .ToList();
        ViewBag.Message = "Student accounts must be created by Admin. Use the course page to enroll existing students into your courses.";

        var model = new StudentViewModel { InstructorId = instructorId };
        return View(model);
    }

    // POST: Handle enroll existing student only (no account creation)
    [HttpPost]
    public async Task<IActionResult> EnrollStudents(StudentViewModel model)
    {
        // This endpoint no longer creates User accounts. Only admin can create students.
        TempData["Error"] = "Only Admin can register new student accounts. Please ask admin to create a student, or use the course's Enroll feature to enroll existing students.";
        return RedirectToAction("EnrollStudents");
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
        var username = User.Identity?.Name;
        var loggedInInstructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User!.Username == username);

        var instructorId = loggedInInstructor?.id ?? 0;

        if (!loggedInInstructor.IsApproved)
        {
            TempData["ErrorMessage"] = "Your instructor account is awaiting admin approval. You cannot create courses yet.";
            return RedirectToAction("Dashboard");
        }

        if (!loggedInInstructor.IsCourseCreationAllowed)
        {
            TempData["ErrorMessage"] = "An administrator must grant permission before you can create courses.";
            return RedirectToAction("Dashboard");
        }

        ViewBag.Instructors = new SelectList(_dbContext.Instructors.ToList(), "id", "FirstName", instructorId);
        
        var model = new CourseViewModels 
        { 
            InstructorId = instructorId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddYears(1)
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(CourseViewModels model)
    {
        var username = User.Identity?.Name;
        var loggedInInstructor = _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefault(i => i.User!.Username == username);

        if (loggedInInstructor == null)
        {
            TempData["ErrorMessage"] = "Unauthorized";
            return RedirectToAction("Dashboard");
        }

        if (!loggedInInstructor.IsApproved)
        {
            TempData["ErrorMessage"] = "Your instructor account is awaiting admin approval. You cannot create courses yet.";
            return RedirectToAction("Dashboard");
        }

        if (!loggedInInstructor.IsCourseCreationAllowed)
        {
            TempData["ErrorMessage"] = "An administrator must grant permission before you can create courses.";
            return RedirectToAction("Dashboard");
        }

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

        // Log activity: instructor created a course
        var instructorUsername = User.Identity?.Name;
        var instructorUser = _dbContext.Users.FirstOrDefault(u => u.Username == instructorUsername);
        _dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = instructorUser?.Id.ToString(),
            ActivityType = ActivityType.CreateCourse,
            Timestamp = DateTimeOffset.UtcNow,
            ResourceId = course.Id.ToString(),
            CourseId = course.Id.ToString()
        });

        // Note: Do NOT auto-assign or auto-enroll students when creating a course.
        // Students must be explicitly enrolled by the instructor via the Enroll student flow.

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
            Quizzes = course.Quizzes?.ToList(),

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
                Title = model.Title, // Use Assignment Title instead of FileName
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

    [HttpGet]
    public async Task<IActionResult> ViewQuizSubmissions(int quizId)
    {
        var quiz = await _dbContext.Quizzes
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null)
            return NotFound();

        var submissions = await _dbContext.QuizSubmissions
            .Include(s => s.Student)
            .Include(s => s.Quiz)
            .Where(s => s.QuizId == quizId)
            .ToListAsync();

        ViewBag.QuizTitle = quiz.Title;
        return View(submissions);
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






    public async Task<IActionResult> Profile()
    {
        var username = User.Identity?.Name;
        var instructor = await _dbContext.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.User!.Username == username);

        if (instructor == null)
            return NotFound();

        return View(instructor);
    }
}
