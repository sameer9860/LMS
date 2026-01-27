using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LMS.Models;

namespace LMS.Models
{
    public class QuizSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }
        [ForeignKey("QuizId")]
        public virtual Quiz? Quiz { get; set; }

        [Required]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; }

        public int? Score { get; set; }

        public int? TotalQuestions { get; set; }

        // JSON string storing answers as {"questionId": "answer"}
        public string? AnswersJson { get; set; }

        // Navigation property for answers
        public virtual ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
    }

    public class QuizAnswer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizSubmissionId { get; set; }
        [ForeignKey("QuizSubmissionId")]
        public virtual QuizSubmission? Submission { get; set; }

        [Required]
        public int MCQId { get; set; }
        [ForeignKey("MCQId")]
        public virtual MCQ? MCQ { get; set; }

        [Required]
        public string? StudentAnswer { get; set; }

        public bool IsCorrect { get; set; }
    }
}
