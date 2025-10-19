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
        [HttpGet]
        public IActionResult AutoQuiz(int courseId)
        {
            var model = new QuizViewModel { CourseId = courseId, Type = QuizType.AI };
            return View(model);
        }

        [HttpPost]
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

                // TODO: Extract text from PDF or DOCX
                materialText = "Extracted text from uploaded PDF";
            }

            // Generate AI Quiz
            var quiz = await _aiQuizService.GenerateQuizFromMaterialAsync(materialText, 10, model.CourseId);
            quiz.MaterialPath = materialPath;

            _dbContext.Quizzes.Add(quiz);
            await _dbContext.SaveChangesAsync();

            TempData["Success"] = "AI quiz generated successfully!";
            return RedirectToAction("CourseDetails", "Instructor", new { id = model.CourseId });
        }

        // ================= Manual Quiz =================
        [HttpGet]
        public IActionResult ManualQuiz(int courseId)
        {
            var model = new QuizViewModel { CourseId = courseId, Type = QuizType.Traditional };
            return View(model);
        }

        [HttpPost]
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

            TempData["Success"] = "Manual quiz created successfully!";
            return RedirectToAction("CourseDetails", "Instructor", new { id = model.CourseId });
        }

        // ================= View Quiz =================
        [HttpGet]
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
        public async Task<IActionResult> TakeQuiz(int quizId)
        {
            var quiz = await _dbContext.Quizzes
                .Include(q => q.MCQs)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            // Randomize options for students
            var randomizedMCQs = quiz.MCQs.Select(q =>
            {
                var options = new List<string> { q.OptionA!, q.OptionB!, q.OptionC!, q.OptionD! };
                if (!string.IsNullOrEmpty(q.OptionE)) options.Add(q.OptionE);

                return new QuizTakeViewModel
                {
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
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
    }
}
