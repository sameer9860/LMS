using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LMS.Views.Data;
using LMS.Models;
using LMS.Services;
using System.Text.Json;

public class StudentController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IActivityService _activity;

    public StudentController(
        ApplicationDbContext dbContext,
        IWebHostEnvironment webHostEnvironment,
        IActivityService activity)
    {
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
        _activity = activity;
    }

    // ---------------------- LOG HELPER ----------------------
    private async Task LogAsync(ActivityType type, string? courseId = null, string? resourceId = null, object? meta = null)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var agent = Request.Headers["User-Agent"].ToString();

        var log = new ActivityLog
        {
            UserId = User.Identity?.Name,
            CourseId = courseId,
            ResourceId = resourceId,
            ActivityType = type,
            Timestamp = DateTimeOffset.Now,
            IpAddress = ip,
            UserAgent = agent,
            MetadataJson = meta != null ? JsonSerializer.Serialize(meta) : null
        };

        await _activity.LogAsync(log);
    }

    // ---------------------- DASHBOARD ------------------------
    public async Task<IActionResult> Dashboard()
    {
        await LogAsync(ActivityType.Login);

        var username = User.Identity?.Name;

        var student = _dbContext.Students?
            .Include(s => s.Enrollments!)
            .ThenInclude(e => e.Course)
            .FirstOrDefault(s => s.User!.Username == username);

        int enrolledCount = student?.Enrollments?.Count ?? 0;
        ViewBag.MyCoursesCount = enrolledCount;

        return View();
    }

    // ---------------------- MY COURSES ------------------------
    public async Task<IActionResult> MyCourses()
    {
        var username = User.Identity?.Name;

        var student = _dbContext.Students?
            .Include(s => s.User!)
            .Include(s => s.Enrollments!)
                .ThenInclude(e => e.Course)
            .FirstOrDefault(s => s.User!.Username == username);

        if (student == null)
            return Unauthorized();

        await LogAsync(ActivityType.ViewMaterial, null, null, new { Page = "MyCourses" });

        var courses = student.Enrollments!
            .Where(e => e.Course != null)
            .Select(e => e.Course)
            .ToList();

        return View(courses);
    }

    // ---------------------- COURSE DETAILS ------------------------
    public async Task<IActionResult> CourseDetails(int id)
    {
        var course = _dbContext.Courses
            .Include(c => c.Assignments!).ThenInclude(a => a.Materials)
            .Include(c => c.Enrollments!).ThenInclude(e => e.Student)
            .Include(c => c.Materials)
            .Include(c => c.ChatMessages!).ThenInclude(m => m.User)
            .Include(c => c.LiveClasses)
            .Include(c => c.Quizzes)
            .FirstOrDefault(c => c.Id == id);

        if (course == null)
            return NotFound();

        await LogAsync(ActivityType.ViewMaterial, id.ToString(), null);

        var username = User.Identity?.Name;
        var student = _dbContext.Students
            .Include(s => s.User)
            .FirstOrDefault(s => s.User!.Username == username);

        if (student == null)
            return Unauthorized();

        var submissions = _dbContext.AssignmentSubmissions
            .Include(s => s.Assignment)
            .Where(s => s.StudentId == student.Id && s.Assignment!.CourseId == id)
            .ToList();

        // Live class status update
        foreach (var liveClass in course.LiveClasses!)
        {
            if (DateTime.Now >= liveClass.StartTime &&
                DateTime.Now <= liveClass.EndTime)
            {
                liveClass.IsLive = true;
                liveClass.IsCompleted = false;
            }
            else if (DateTime.Now > liveClass.EndTime)
            {
                liveClass.IsLive = false;
                liveClass.IsCompleted = true;
            }
        }

        // Chat messages sorted
        ViewBag.ChatMessages = _dbContext.ChatMessages
            .Include(m => m.User)
            .Where(m => m.CourseId == id)
            .OrderBy(m => m.SentAt)
            .ToList();

        ViewBag.CurrentStudentId = student.Id;
        ViewBag.Submissions = submissions;

        return View(course);
    }

    // ---------------------- VIEW MATERIAL ------------------------
    public async Task<IActionResult> ViewMaterial(int id)
    {
        var material = _dbContext.Materials
            .Include(m => m.Course)
            .FirstOrDefault(m => m.Id == id);

        if (material == null)
            return NotFound();

        await LogAsync(ActivityType.ViewMaterial, material.CourseId.ToString(), id.ToString());

        return View(material);
    }

    // ---------------------- DOWNLOAD MATERIAL ------------------------
    public async Task<IActionResult> DownloadMaterial(int id)
    {
        var material = _dbContext.Materials
            .FirstOrDefault(m => m.Id == id);

        if (material == null)
            return NotFound();

        await LogAsync(ActivityType.DownloadMaterial, material.CourseId.ToString(), id.ToString());

        var path = Path.Combine(_webHostEnvironment.WebRootPath, material.FilePath!);
        var fileBytes = await System.IO.File.ReadAllBytesAsync(path);

        return File(fileBytes, "application/octet-stream", Path.GetFileName(path));
    }

    // ---------------------- JOIN LIVE CLASS ------------------------
    public async Task<IActionResult> JoinLiveClass(int id)
    {
        var liveClass = _dbContext.LiveClasses
            .Include(l => l.Course)
            .FirstOrDefault(l => l.Id == id);

        if (liveClass == null)
            return NotFound();

        await LogAsync(ActivityType.JoinLiveClass, liveClass.CourseId.ToString(), id.ToString());

        return View("JoinLiveClass", liveClass);
    }

    // ---------------------- LEAVE LIVE CLASS ------------------------
    public async Task<IActionResult> LeaveLiveClass(int id)
    {
        var liveClass = _dbContext.LiveClasses.FirstOrDefault(l => l.Id == id);
        if (liveClass == null)
            return NotFound();

        await LogAsync(ActivityType.LeaveLiveClass, liveClass.CourseId.ToString(), id.ToString());

        return RedirectToAction("CourseDetails", new { id = liveClass.CourseId, tab = "live-classes" });
    }

    // ---------------------- START ASSIGNMENT ------------------------
    public async Task<IActionResult> StartAssignment(int id)
    {
        var assign = _dbContext.Assignments
            .Include(a => a.Course)
            .FirstOrDefault(a => a.Id == id);

        if (assign == null)
            return NotFound();

        await LogAsync(ActivityType.StartAssignment, assign.CourseId.ToString(), id.ToString());

        // Redirect to course assignments tab instead of showing a separate view
        return RedirectToAction("CourseDetails", new { id = assign.CourseId, tab = "assignments" });
    }

    // ---------------------- SUBMIT ASSIGNMENT ------------------------
    [HttpPost]
    public async Task<IActionResult> SubmitAssignment(int AssignmentId, IFormFile SubmissionFile, string AnswerText)
    {
        var username = User.Identity?.Name;
        var student = _dbContext.Students
            .Include(s => s.User)
            .FirstOrDefault(s => s.User!.Username == username);

        if (student == null)
            return Unauthorized();

        if (SubmissionFile == null)
        {
            TempData["ErrorAssignment"] = "Please upload a valid file.";
        }

        try
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "submissions");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(SubmissionFile!.FileName);
            var savePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await SubmissionFile.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("submissions", fileName);

            var submission = new AssignmentSubmission
            {
                AssignmentId = AssignmentId,
                StudentId = student.Id,
                SubmittedAt = DateTime.Now,
                FilePath = relativePath.Replace("\\", "/"),
                AnswerText = AnswerText
            };

            _dbContext.AssignmentSubmissions.Add(submission);
            await _dbContext.SaveChangesAsync();

            // Get assignment and course details to notify instructor
            var assignment = _dbContext.Assignments
                .Include(a => a.Course)
                .FirstOrDefault(a => a.Id == AssignmentId);

            if (assignment?.Course != null)
            {
                var instructor = _dbContext.Instructors
                    .Include(i => i.User)
                    .FirstOrDefault(i => i.id == assignment.Course.Instructorid);

                if (instructor?.User != null)
                {
                    var notification = new Notification
                    {
                        UserId = instructor.User.Id,
                        Title = "New Assignment Submission",
                        Message = $"Student {student.FirstName} {student.LastName} submitted the assignment \"{assignment.Title}\".",
                        NotificationType = "assignment_submission",
                        RelatedId = submission.Id,
                        IconClass = "fas fa-file-upload",
                        ActionUrl = $"/Instructor/ViewSubmissions?assignmentId={AssignmentId}",
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };
                    _dbContext.Notifications.Add(notification);
                    await _dbContext.SaveChangesAsync();
                }
            }

            await LogAsync(ActivityType.SubmitAssignment, submission.AssignmentId.ToString(), submission.Id.ToString());

            TempData["SuccessAssignment"] = "Assignment submitted!";
        }
        catch (Exception)
        {
            TempData["ErrorAssignment"] = "Submission failed!";
        }

        var courseId = _dbContext.Assignments
            .Where(a => a.Id == AssignmentId)
            .Select(a => a.CourseId)
            .FirstOrDefault();

        return RedirectToAction("CourseDetails", new { id = courseId, tab = "assignments" });
    }

    // ---------------------- MY SUBMISSIONS ------------------------
    public IActionResult MySubmissions()
    {
        var username = User.Identity?.Name;

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

    // ---------------------- PROFILE ------------------------
    public async Task<IActionResult> Profile()
    {
        var username = User.Identity?.Name;
        var student = await _dbContext.Students
            .Include(s => s.User)
            .Include(s => s.Instructor)
            .FirstOrDefaultAsync(s => s.User!.Username == username);

        if (student == null)
            return NotFound();

        return View(student);
    }
}
