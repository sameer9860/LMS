using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    // Quiz type enum
    public enum QuizType
    {
        Traditional = 0,
        AI = 1
    }

    // Quiz entity
    public class Quiz
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        // AI generated or manual
        [Required]
        public QuizType Type { get; set; }

        // Relation to course
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public string? MaterialPath { get; set; }


        // Collection of questions
        public virtual ICollection<MCQ> MCQs { get; set; } = new List<MCQ>();
    }

    // MCQ entity
    public class MCQ
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Question { get; set; }

        [Required]
        public string? OptionA { get; set; }

        [Required]
        public string? OptionB { get; set; }

        [Required]
        public string? OptionC { get; set; }

        [Required]
        public string? OptionD { get; set; }

        public string? OptionE { get; set; } // optional

        [Required]
        public string? CorrectAnswer { get; set; } // e.g., "A", "B", etc.

        public string? Feedback { get; set; }

        // Foreign key
        public int QuizId { get; set; }
        public virtual Quiz? Quiz { get; set; }
    }
}
