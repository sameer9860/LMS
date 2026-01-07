using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using LMS.Models;

namespace LMS.ViewModels
{
    public class CourseViewModels
    {
        public int Id { get; set; }

        [Required]
        public string? FullName { get; set; }

        [Required]
        public string? ShortName { get; set; }

        [Required]
        public string? Category { get; set; }

        [Required]
        public bool IsVisible { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string? CourseIdNumber { get; set; }

        public string? Summary { get; set; }

        public IFormFile? CourseImage { get; set; }
        [Required]
        public int InstructorId { get; set; }
        public Instructor? Instructor { get; set; }
        
       

    }
}