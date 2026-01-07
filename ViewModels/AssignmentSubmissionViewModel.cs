using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LMS.ViewModels
{
    public class AssignmentSubmissionViewModel
    {
        [Required]
        public int AssignmentId { get; set; }

        public int StudentId { get; set; }

        [Required]
        public IFormFile? SubmissionFile { get; set; }
        public string? AnswerText { get; set; } // Optional text answer

        public DateTime SubmittedAt { get; set; }

        public int? MarksObtained { get; set; }
        public bool? IsPassed { get; set; }
        public string? Feedback { get; set; }

        
    }
}
