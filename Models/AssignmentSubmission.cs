using LMS.Models;

using System;
using System.ComponentModel.DataAnnotations;
namespace LMS.Models
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }

    [Required]
    public int AssignmentId { get; set; }

    [Required]
    public int StudentId { get; set; }

    public string? FilePath { get; set; }
    public string? AnswerText { get; set; } // Optional text answer
    public DateTime SubmittedAt { get; set; }

    // Grading info
    public int? MarksObtained { get; set; }
    public bool? IsPassed { get; set; }

    public string? Feedback { get; set; }

    public Assignment? Assignment { get; set; }
    public Student? Student { get; set; }
    }
}