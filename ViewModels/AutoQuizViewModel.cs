using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LMS.ViewModels
{
    public class AutoQuizViewModel
    {
        public int CourseId { get; set; }
        [Required]
        public IFormFile? PdfFile { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}