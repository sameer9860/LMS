using LMS.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMS.ViewModels
{
    public class QuizViewModel
    {
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public QuizType Type { get; set; }

        public int CourseId { get; set; }

        public IFormFile? MaterialFile { get; set; }

        public List<MCQViewModel> Questions { get; set; } = new List<MCQViewModel>();
    }

    public class MCQViewModel
    {
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

        public string? OptionE { get; set; }

        [Required]
        public string? CorrectAnswer { get; set; }

        public string? Feedback { get; set; }
    }
}
