using LMS.Models;
using LMS.Services;
using LMS.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LMS.Views.Data;

namespace LMS.Controllers
{
    [Route("[controller]")]
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAIQuizService _aiQuizService;
        private readonly IWebHostEnvironment _env;

        public QuizController(ApplicationDbContext dbContext, IAIQuizService aiQuizService, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _aiQuizService = aiQuizService;
            _env = env;
        }

        // ================= Auto Quiz =================
        [HttpGet("AutoQuiz")]
        public IActionResult AutoQuiz(int courseId)
        {
            var model = new QuizViewModel { CourseId = courseId, Type = QuizType.AI };
            return View(model);
        }

        [HttpPost("AutoQuiz")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoQuiz(QuizViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string materialText = "";
            string? materialPath = null;

            if (model.MaterialFile != null && model.MaterialFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}_{model.MaterialFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.MaterialFile.CopyToAsync(fileStream);
                }

                materialPath = "/uploads/" + fileName;

                // For now, use a simple placeholder text
                // In production, you'd extract text from PDF using a library like iTextSharp or PdfSharp
                materialText = model.MaterialFile.FileName;
            }

            // Generate AI Quiz with OpenAI
            var quiz = await _aiQuizService.GenerateQuizFromMaterialAsync(materialText, 10, model.CourseId);
            
            // Set quiz properties from model
            quiz.Title = model.Title ?? "Auto-Generated Quiz";
            quiz.Description = model.Description ?? "Generated automatically from uploaded material";
            quiz.DueDate = model.DueDate != DateTime.MinValue ? model.DueDate : DateTime.Now.AddDays(7);
            quiz.MaterialPath = materialPath;
            quiz.Type = QuizType.AI;

            _dbContext.Quizzes.Add(quiz);
            await _dbContext.SaveChangesAsync();

            TempData["SuccessAssignment"] = "AI quiz generated successfully!";
            return RedirectToAction("CourseDetails", "Instructor", new { id = model.CourseId });
        }

        // ================= Manual Quiz =================
        [HttpGet("ManualQuiz")]
        public IActionResult ManualQuiz(int courseId)
        {
            var model = new QuizViewModel 
            { 
                CourseId = courseId, 
                Type = QuizType.Traditional,
                DueDate = DateTime.Now.AddDays(7)
            };
            return View(model);
        }

        [HttpPost("ManualQuiz")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualQuiz(QuizViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string? materialPath = null;
            if (model.MaterialFile != null && model.MaterialFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}_{model.MaterialFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.MaterialFile.CopyToAsync(fileStream);
                }

                materialPath = "/uploads/" + fileName;
            }

            var quiz = new Quiz
            {
                Title = model.Title,
                Description = model.Description,
                DueDate = model.DueDate,
                Type = QuizType.Traditional,
                CourseId = model.CourseId,
                MaterialPath = materialPath,
                MCQs = model.Questions.Select(q => new MCQ
                {
                    Question = q.Question,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    OptionE = q.OptionE,
                    CorrectAnswer = q.CorrectAnswer,
                    Feedback = q.Feedback
                }).ToList()
            };

            _dbContext.Quizzes.Add(quiz);
            await _dbContext.SaveChangesAsync();

            // Create notifications for enrolled students
            var enrolledStudents = _dbContext.Enrollments
                .Where(e => e.CourseId == model.CourseId)
                .Include(e => e.Student)
                .ThenInclude(s => s.User)
                .Select(e => e.Student!.User)
                .ToList();

            foreach (var user in enrolledStudents)
            {
                _dbContext.Notifications.Add(new Notification
                {
                    UserId = user!.Id,
                    Title = "New Quiz",
                    Message = $"A new quiz \"{quiz.Title}\" has been added.",
                    NotificationType = "quiz",
                    RelatedId = quiz.Id,
                    IconClass = "fas fa-graduation-cap",
                    ActionUrl = $"/Quiz/TakeQuiz?id={quiz.Id}",
                    CreatedAt = DateTime.Now
                });
            }

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "Manual quiz created successfully!";
            return RedirectToAction("CourseDetails", "Instructor", new { id = model.CourseId });
        }

        // ================= View Quiz =================
        [HttpGet("ViewQuiz")]
        public async Task<IActionResult> ViewQuiz(int quizId)
        {
            var quiz = await _dbContext.Quizzes
                .Include(q => q.MCQs)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            // Randomize options for each question
            var randomizedMCQs = quiz.MCQs.Select(q =>
            {
                var options = new List<string> { q.OptionA!, q.OptionB!, q.OptionC!, q.OptionD! };
                if (!string.IsNullOrEmpty(q.OptionE)) options.Add(q.OptionE);

                return new
                {
                    q.Question,
                    Options = options.OrderBy(x => Guid.NewGuid()).ToList(),
                    q.CorrectAnswer,
                    q.Feedback
                };
            }).ToList();

            ViewBag.Quiz = quiz;
            ViewBag.MCQs = randomizedMCQs;

            return View(quiz);
        }

        // ================= Take Quiz =================
        [HttpGet]
        [Route("[action]/{id}")]
        [Route("[action]")]
        public async Task<IActionResult> TakeQuiz(int id)
        {
            var quiz = await _dbContext.Quizzes
                .Include(q => q.MCQs)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();

            // Randomize options for students
            var randomizedMCQs = quiz.MCQs.Select(q =>
            {
                var options = new List<string> { q.OptionA!, q.OptionB!, q.OptionC!, q.OptionD! };
                if (!string.IsNullOrEmpty(q.OptionE)) options.Add(q.OptionE);

                return new QuizTakeViewModel
                {
                    Id = q.Id,
                    Question = q.Question!,
                    Options = options.OrderBy(x => Guid.NewGuid()).ToList(),
                    CorrectAnswer = q.CorrectAnswer!,
                    Feedback = q.Feedback!
                };
            }).ToList();

            var model = new TakeQuizViewModel
            {
                QuizId = quiz.Id,
                Title = quiz.Title!,
                MCQs = randomizedMCQs
            };

            return View(model);
        }

        // ================= Submit Quiz =================
        [HttpPost("SubmitQuiz")]
        public async Task<IActionResult> SubmitQuiz([FromBody] QuizSubmissionViewModel model)
        {
            if (model == null || model.QuizId == 0)
                return BadRequest("Invalid quiz data");

            var username = User.Identity?.Name;
            var student = await _dbContext.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User!.Username == username);

            if (student == null)
                return Unauthorized();

            var quiz = await _dbContext.Quizzes
                .Include(q => q.MCQs)
                .FirstOrDefaultAsync(q => q.Id == model.QuizId);

            if (quiz == null)
                return NotFound();

            // Calculate score
            int correctAnswers = 0;
            var answers = new List<QuizAnswer>();

            foreach (var submittedAnswer in model.Answers)
            {
                var mcq = quiz.MCQs.FirstOrDefault(q => q.Id == submittedAnswer.MCQId);
                if (mcq != null)
                {
                    bool isCorrect = mcq.CorrectAnswer.Equals(submittedAnswer.Answer, StringComparison.OrdinalIgnoreCase);
                    if (isCorrect) correctAnswers++;

                    answers.Add(new QuizAnswer
                    {
                        MCQId = submittedAnswer.MCQId,
                        StudentAnswer = submittedAnswer.Answer,
                        IsCorrect = isCorrect
                    });
                }
            }

            // Create submission record
            var submission = new QuizSubmission
            {
                QuizId = model.QuizId,
                StudentId = student.Id,
                SubmittedAt = DateTime.Now,
                Score = correctAnswers,
                TotalQuestions = quiz.MCQs.Count
            };

            _dbContext.QuizSubmissions.Add(submission);
            await _dbContext.SaveChangesAsync();

            // Now set the QuizSubmissionId for all answers and add them
            foreach (var answer in answers)
            {
                answer.QuizSubmissionId = submission.Id;
            }
            _dbContext.QuizAnswers.AddRange(answers);
            await _dbContext.SaveChangesAsync();

            // Calculate percentage
            double percentage = (correctAnswers * 100.0) / quiz.MCQs.Count;

            return Ok(new
            {
                success = true,
                score = correctAnswers,
                totalQuestions = quiz.MCQs.Count,
                percentage = Math.Round(percentage, 2),
                submissionId = submission.Id
            });
        }

        // ================= View Quiz Result =================
        [HttpGet("QuizResult/{submissionId}")]
        public async Task<IActionResult> QuizResult(int submissionId)
        {
            var submission = await _dbContext.QuizSubmissions
                .Include(s => s.Quiz)
                .ThenInclude(q => q.MCQs)
                .Include(s => s.Answers)
                .ThenInclude(a => a.MCQ)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                return NotFound();

            // Check if user is the student or instructor of the course
            var username = User.Identity?.Name;
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

            // Build the result view model
            var score = submission.Score ?? 0;
            var totalQuestions = submission.TotalQuestions ?? 0;
            var percentage = totalQuestions > 0 ? (score * 100.0) / totalQuestions : 0;
            
            var answerDetails = submission.Answers.Select(a => new QuizAnswerDetail
            {
                StudentAnswer = a.StudentAnswer,
                IsCorrect = a.IsCorrect,
                CorrectAnswer = a.MCQ?.CorrectAnswer ?? "N/A"
            }).ToList();

            var questionDetails = submission.Quiz?.MCQs.Select(q => new QuestionDetail
            {
                Question = q.Question
            }).ToList() ?? new List<QuestionDetail>();

            var model = new QuizResultViewModel
            {
                QuizTitle = submission.Quiz?.Title ?? "Quiz",
                Score = score,
                TotalQuestions = totalQuestions,
                Percentage = Math.Round(percentage),
                SubmittedAt = submission.SubmittedAt,
                Answers = answerDetails,
                QuestionDetails = questionDetails
            };

            return View(model);
        }

        // ================= Delete Quiz =================
        [HttpPost("DeleteQuiz")]
        public async Task<IActionResult> DeleteQuiz(int quizId)
        {
            var quiz = await _dbContext.Quizzes
                .Include(q => q.MCQs)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return Json(new { success = false, message = "Quiz not found" });

            // Verify authorization (check if user is instructor of the course)
            var username = User.Identity?.Name;
            var instructor = await _dbContext.Instructors
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.User!.Username == username);

            if (instructor == null)
                return Json(new { success = false, message = "Unauthorized" });

            var course = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == quiz.CourseId && c.Instructorid == instructor.id);
            if (course == null)
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                // Remove all notifications related to this quiz
                var notifications = _dbContext.Notifications
                    .Where(n => n.RelatedId == quizId && n.NotificationType == "quiz")
                    .ToList();
                _dbContext.Notifications.RemoveRange(notifications);

                // Remove all quiz submissions and their answers
                var submissions = _dbContext.QuizSubmissions
                    .Include(s => s.Answers)
                    .Where(s => s.QuizId == quizId)
                    .ToList();

                foreach (var submission in submissions)
                {
                    _dbContext.QuizAnswers.RemoveRange(submission.Answers);
                }
                _dbContext.QuizSubmissions.RemoveRange(submissions);

                // Remove the quiz (MCQs will be removed by cascade delete)
                _dbContext.Quizzes.Remove(quiz);
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Quiz deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting quiz: {ex.Message}" });
            }
        }

        // ================= Edit Quiz (GET) =================
        [HttpGet("EditQuiz")]
        public async Task<IActionResult> EditQuiz(int quizId)
        {
            var quiz = await _dbContext.Quizzes
                .Include(q => q.MCQs)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return NotFound();

            // Verify authorization
            var username = User.Identity?.Name;
            var instructor = await _dbContext.Instructors
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.User!.Username == username);

            if (instructor == null)
                return Unauthorized();

            var course = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == quiz.CourseId && c.Instructorid == instructor.id);
            if (course == null)
                return Unauthorized();

            // Map to QuizViewModel for editing
            var model = new QuizViewModel
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                CourseId = quiz.CourseId,
                DueDate = quiz.DueDate,
                Type = quiz.Type,
                Questions = quiz.MCQs.Select(q => new MCQViewModel
                {
                    Question = q.Question,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    OptionE = q.OptionE,
                    CorrectAnswer = q.CorrectAnswer,
                    Feedback = q.Feedback
                }).ToList()
            };

            return View(model);
        }

        // ================= Edit Quiz (POST) =================
        [HttpPost("EditQuiz")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuiz(QuizViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var quiz = await _dbContext.Quizzes
                .Include(q => q.MCQs)
                .FirstOrDefaultAsync(q => q.Id == model.Id);

            if (quiz == null)
                return NotFound();

            // Verify authorization
            var username = User.Identity?.Name;
            var instructor = await _dbContext.Instructors
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.User!.Username == username);

            if (instructor == null)
                return Unauthorized();

            var course = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == quiz.CourseId && c.Instructorid == instructor.id);
            if (course == null)
                return Unauthorized();

            try
            {
                // Update quiz properties
                quiz.Title = model.Title;
                quiz.Description = model.Description;
                quiz.DueDate = model.DueDate;

                // Remove old MCQs
                _dbContext.MCQs.RemoveRange(quiz.MCQs);

                // Add new MCQs
                quiz.MCQs = model.Questions.Select(q => new MCQ
                {
                    Question = q.Question,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    OptionE = q.OptionE,
                    CorrectAnswer = q.CorrectAnswer,
                    Feedback = q.Feedback
                }).ToList();

                _dbContext.Quizzes.Update(quiz);
                await _dbContext.SaveChangesAsync();

                TempData["Success"] = "Quiz updated successfully!";
                return RedirectToAction("CourseDetails", "Instructor", new { id = quiz.CourseId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating quiz: {ex.Message}");
                return View(model);
            }
        }

        // ================= View Quiz Submissions (Instructor) =================
        [HttpGet("QuizSubmissions")]
        public async Task<IActionResult> QuizSubmissions(int quizId)
        {
            var quiz = await _dbContext.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null)
                return NotFound();

            // Verify authorization (instructor of the course)
            var username = User.Identity?.Name;
            var instructor = await _dbContext.Instructors
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.User!.Username == username);

            if (instructor == null || quiz.Course.Instructorid != instructor.id)
                return Unauthorized();

            var submissions = await _dbContext.QuizSubmissions
                .Where(s => s.QuizId == quizId)
                .Include(s => s.Student)
                .ThenInclude(st => st.User)
                .Include(s => s.Quiz)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            ViewBag.Quiz = quiz;
            return View(submissions);
        }

        // ================= Student Quiz Scores =================
        [HttpGet("StudentQuizScores")]
        public async Task<IActionResult> StudentQuizScores(int courseId)
        {
            var username = User.Identity?.Name;
            var student = await _dbContext.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User!.Username == username);

            if (student == null)
                return Unauthorized();

            // Verify student is enrolled in the course
            var enrollment = await _dbContext.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId);

            if (enrollment == null)
                return Unauthorized();

            var course = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseId);

            var quizzes = await _dbContext.Quizzes
                .Where(q => q.CourseId == courseId)
                .ToListAsync();

            var quizScores = new List<StudentQuizScoreViewModel>();

            foreach (var quiz in quizzes)
            {
                var submission = await _dbContext.QuizSubmissions
                    .FirstOrDefaultAsync(s => s.QuizId == quiz.Id && s.StudentId == student.Id);

                quizScores.Add(new StudentQuizScoreViewModel
                {
                    QuizId = quiz.Id,
                    QuizTitle = quiz.Title,
                    QuizDescription = quiz.Description,
                    Score = submission?.Score ?? 0,
                    TotalQuestions = submission?.TotalQuestions ?? 0,
                   Percentage = submission != null && submission.TotalQuestions > 0
    ? Math.Round((submission.Score ?? 0d) * 100.0 / (double)submission.TotalQuestions, 2)
    : 0,
                    SubmittedAt = submission?.SubmittedAt,
                    HasSubmitted = submission != null
                });
            }

            ViewBag.Course = course;
            return View(quizScores);
        }
    }

    // ViewModel for quiz submission
    public class QuizSubmissionViewModel
    {
        public int QuizId { get; set; }
        public List<SubmittedAnswerViewModel> Answers { get; set; } = new();
    }

    public class SubmittedAnswerViewModel
    {
        public int MCQId { get; set; }
        public string Answer { get; set; } = string.Empty;
    }

    // ViewModel for taking a quiz
    public class TakeQuizViewModel
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<QuizTakeViewModel> MCQs { get; set; } = new();
    }

    public class QuizTakeViewModel
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
    }

    public class QuizResultViewModel
    {
        public string QuizTitle { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public double Percentage { get; set; }
        public DateTime SubmittedAt { get; set; }
        public List<QuizAnswerDetail> Answers { get; set; } = new();
        public List<QuestionDetail> QuestionDetails { get; set; } = new();
    }

    public class QuizAnswerDetail
    {
        public string StudentAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    public class QuestionDetail
    {
        public string Question { get; set; } = string.Empty;
    }

    public class StudentQuizScoreViewModel
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public string? QuizDescription { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public double Percentage { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool HasSubmitted { get; set; }
    }}