using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using LMS.Views.Data; // Add this line or replace LMS.Data with the correct namespace for ApplicationDbContext
using LMS.Models; // Add this line or replace with the correct namespace for AssignmentSubmission

public class StudentController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public StudentController(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment)
    {
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Student/Dashboard
 public IActionResult Dashboard()
{
    var username = User.Identity?.Name;
    var student = _dbContext.Students
        .Include(s => s.Enrollments!)
        .ThenInclude(e => e.Course)
        .FirstOrDefault(s => s.User!.Username == username);

    int enrolledCourseCount = student?.Enrollments?.Count ?? 0;
    ViewBag.MyCoursesCount = enrolledCourseCount;

    return View();
}

    [HttpGet]
    public IActionResult MyCourses()
    {
        var username = User.Identity?.Name;
        var student = _dbContext.Students
            .Include(s => s.User)
            .Include(s => s.Enrollments!)
                .ThenInclude(e => e.Course)
            .FirstOrDefault(s => s.User!.Username == username);

        if (student == null)
        {
            TempData["ErrorMessage"] = "Unauthorized access.";
            return RedirectToAction("Login", "Account");
        }

        var courses = student.Enrollments!
            .Where(e => e.Course != null)
            .Select(e => e.Course!)
            .ToList();

        return View(courses);
    }
public IActionResult CourseDetails(int id)
{
    // Load course with related entities
    var course = _dbContext.Courses
            .Include(c => c.Assignments!)
            .ThenInclude(a => a.Materials)
            .Include(c => c.Enrollments!)
            .ThenInclude(e => e.Student)
            .Include(c => c.Materials)
            .Include(c => c.ChatMessages!)
            .ThenInclude(m => m.User) // include User to avoid lazy loading issues
        .Include(c => c.LiveClasses)
        .FirstOrDefault(c => c.Id == id);

    if (course == null)
        return NotFound();

    // Get the current student
    var username = User.Identity?.Name;
    var student = _dbContext.Students
        .Include(s => s.User)
        .FirstOrDefault(s => s.User!.Username == username);

    if (student == null)
        return Unauthorized();

    // Load student submissions for this course
    var submissions = _dbContext.AssignmentSubmissions
        .Include(s => s.Assignment)
        .Where(s => s.StudentId == student.Id && s.Assignment!.CourseId == id)
        .ToList();

    // Update LiveClass status dynamically
    foreach (var liveClass in course.LiveClasses!)
    {
        if (DateTime.Now >= liveClass.StartTime && DateTime.Now <= liveClass.EndTime)
        {
            liveClass.IsLive = true;
            liveClass.IsCompleted = false;
        }
        else if (DateTime.Now > liveClass.EndTime)
        {
            liveClass.IsLive = false;
            liveClass.IsCompleted = true;
        }
        else
        {
            liveClass.IsLive = false;
            liveClass.IsCompleted = false; // upcoming
        }
    }

    // Get chat messages sorted by time
  var chatMessages = _dbContext.ChatMessages
    .Include(m => m.User)
    .Where(m => m.CourseId == id)
    .OrderBy(m => m.SentAt)
    .ToList();

ViewBag.ChatMessages = chatMessages;


    // Pass data to ViewBag
    ViewBag.CurrentStudentId = student.Id;
    ViewBag.Submissions = submissions;

    return View(course);
}



public IActionResult JoinLiveClass(int id)
{
    var liveClass = _dbContext.LiveClasses
        .Include(l => l.Course)
        .FirstOrDefault(l => l.Id == id);

    if (liveClass == null)
        return NotFound();

    // Optional: restrict joining only if current time is within the schedule
    if (DateTime.Now < liveClass.StartTime || DateTime.Now > liveClass.EndTime)
    {
        TempData["Error"] = "Live class is not currently active.";
        return RedirectToAction("CourseDetails", new { id = liveClass.CourseId });
    }

    return View("JoinLiveClass", liveClass); // Uses Views/Student/JoinLiveClass.cshtml
}


public IActionResult LeaveLiveClass(int id)
{
    var liveClass = _dbContext.LiveClasses.FirstOrDefault(l => l.Id == id);
    if (liveClass == null)
        return NotFound();

    // Simply redirecting back to CourseDetails (tab=liveclass)
    return RedirectToAction("CourseDetails", new { id = liveClass.CourseId, tab = "live-classes" });
}




    [HttpPost]
public async Task<IActionResult> SubmitAssignment(int AssignmentId, IFormFile SubmissionFile, string AnswerText)
{
    var username = User.Identity?.Name;
    var student = _dbContext.Students
        .Include(s => s.User)
        .FirstOrDefault(s => s.User!.Username == username);

    if (student == null)
    {
        TempData["ErrorAssignment"] = "Unauthorized access.";
        return RedirectToAction("Login", "Account");
    }

    // Validate file
    if (SubmissionFile == null || SubmissionFile.Length == 0)
    {
        TempData["ErrorAssignment"] = "Please upload a valid file.";
        var fallbackCourseId = _dbContext.Assignments
            .Where(a => a.Id == AssignmentId)
            .Select(a => a.CourseId)
            .FirstOrDefault();

        return RedirectToAction("CourseDetails", new { id = fallbackCourseId, tab = "assignments" });
    }

    try
    {
        // Prepare upload path
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "submissions");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid() + Path.GetExtension(SubmissionFile.FileName);
        var savePath = Path.Combine(uploadsFolder, uniqueFileName);
        var relativePath = Path.Combine("submissions", uniqueFileName); // store in DB

        // Save file
        using (var stream = new FileStream(savePath, FileMode.Create))
        {
            await SubmissionFile.CopyToAsync(stream);
        }

        // Save submission
        var submission = new AssignmentSubmission
        {
            AssignmentId = AssignmentId,
            StudentId = student.Id,
            SubmittedAt = DateTime.Now,
            FilePath = relativePath.Replace("\\", "/"), // for cross-platform path
            AnswerText = AnswerText,
            MarksObtained = null,
            IsPassed = null,
            Feedback = null
        };

        _dbContext.AssignmentSubmissions.Add(submission);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessAssignment"] = "Assignment submitted successfully!";
    }
    catch (Exception ex)
    {
        TempData["ErrorAssignment"] = "Something went wrong during submission." + ex.Message;
        // Optionally log ex.Message
    }

    var courseId = _dbContext.Assignments
        .Where(a => a.Id == AssignmentId)
        .Select(a => a.CourseId)
        .FirstOrDefault();

    return RedirectToAction("CourseDetails", new { id = courseId, tab = "assignments" });
}


    [HttpGet]
    public IActionResult MySubmissions()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var student = _dbContext.Students
            .FirstOrDefault(s => s.User!.Username == username);

        if (student == null)
            return Unauthorized();

        var submissions = _dbContext.AssignmentSubmissions
            .Include(s => s.Assignment)
            .Where(s => s.StudentId == student.Id)
            .ToList();

        return View(submissions);
    }
}