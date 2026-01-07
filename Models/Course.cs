using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using LMS.Models;

namespace LMS.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Course Full Name")]
        public string? FullName { get; set; }

        [Required]
        [Display(Name = "Course Short Name")]
        public string? ShortName { get; set; }

        [Required]
        public string? Category { get; set; }

        [Required]
        public bool IsVisible { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Course ID Number")]
        public string? CourseIdNumber { get; set; }

        [Display(Name = "Course Summary")]
        public string? Summary { get; set; }

        [Display(Name = "Course Image")]
        public string? ImagePath { get; set; }

        [ForeignKey("Instructor")]
        public int Instructorid { get; set; }
        public Instructor? Instructor { get; set; }

        public virtual ICollection<Material>? Materials { get; set; }
        public virtual ICollection<Assignment>? Assignments { get; set; }
        
            public virtual ICollection<Quiz>? Quizzes { get; set; } // New

        public virtual ICollection<Enrollment>? Enrollments { get; set; }

        public virtual ICollection<LiveClass>? LiveClasses { get; set; }

        public virtual ICollection<ChatMessage>? ChatMessages{ get; set; }

           

    }

}