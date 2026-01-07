using LMS.Models; // Add this line if User is in LMS.Models namespace
namespace LMS.Models
{
    public class Student
    {
        public int Id { get; set; }

        public required string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public required string LastName { get; set; }

        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }

        public required string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }

        public string? Address { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }

        public required string ProfileImagePath { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public required string Grade { get; set; }

        // Foreign Keys
        public int UserId { get; set; }
        public User? User { get; set; }

        public int Instructorid { get; set; }
        public Instructor? Instructor { get; set; }

        public ICollection<Enrollment>? Enrollments { get; set; }

        

    }
}