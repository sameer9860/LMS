using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.ViewModels
{
    public class AssignmentViewModel
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public IFormFile? File { get; set; }

        public DateTime AssignedDate { get; set; }

        public int PossiblePoints { get; set; } // Total possible score

        public int PassMarks { get; set; } // Minimum score to pass

        [Required]
        public DateTime DueDate { get; set; }

        public string? SubmittedAt { get; set; }
    }
}
