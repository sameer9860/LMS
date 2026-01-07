using System.ComponentModel.DataAnnotations;
using System;

namespace LMS.Models
{
    public class Material
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }
        [Required]
        public string? Title { get; set; }               
        public string? Description { get; set; }
        [Required]
        public string? FilePath { get; set; }
        [Required]
        public string? FileType { get; set; }
        [Required]
        public DateTime UploadDate { get; set; }

        public Course? Course { get; set; }

        
    }
}