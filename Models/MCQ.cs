using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace LMS.Models
{
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
        public string? OptionE { get; set; } // optional 5th option
[Required]
        public string? CorrectAnswer { get; set; } // e.g., "A", "B", "C" etc.
        public string? Feedback { get; set; } // feedback for correct/incorrect answers

        public int QuizId { get; set; }
        public virtual Quiz? Quiz { get; set; }
    }
}
