using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LMS.ViewModels
{
    public class MaterialViewModel
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public IFormFile? File { get; set; } // PDF, video, etc.
    }
}
