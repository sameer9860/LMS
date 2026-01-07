using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{

    public class Enrollment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }              // FK to Student
        public int CourseId { get; set; }
        [Required]
        public DateTime EnrollmentDate { get; set; }

        public Student? Student { get; set; }
        public Course? Course { get; set; }
    }
}