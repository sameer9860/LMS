using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        public int CourseId { get; set; }               // FK to Course
        [Required]
        public string? Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public string? FilePath { get; set; }            // Optional attached file (PDF, docx)
        [Required]
        public DateTime AssignedDate { get; set; }
        [Required]
        public DateTime DueDate { get; set; }
        [Required]
        public int PossiblePoints { get; set; }     // Total possible score

        [Required]
        public int PassMarks { get; set; }          // Minimum score to pass

        public Course? Course { get; set; }
        public virtual ICollection<AssignmentSubmission>? Submissions { get; set; }

        public ICollection<Material>? Materials { get; set; } 


    }
}