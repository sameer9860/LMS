using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMS.ViewModels
{
    public class ManualQuizViewModel
    {
        public int CourseId { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }
        public DateTime DueDate { get; set; }

        public List<MCQInput> Questions { get; set; } = new List<MCQInput>();
    }

    public class MCQInput
    {
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
        public string? OptionE { get; set; }

        [Required]
        public string? CorrectAnswer { get; set; } // A, B, C, D, E
        public string? Feedback { get; set; }
    }
}