using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

public class StudentViewModel
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required]
    public string Gender { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    public string? Address { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianPhone { get; set; }

    [Required]
    public string Grade { get; set; } = string.Empty;

    public DateTime EnrollmentDate { get; set; } = DateTime.Now;

    public IFormFile? ProfileImage { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
[DataType(DataType.Password)]
public string? Password { get; set; }

[Required]
[DataType(DataType.Password)]
[Compare("Password", ErrorMessage = "Passwords do not match.")]
[Display(Name = "Confirm Password")]
public string? ConfirmPassword { get; set; }
    public int? InstructorId { get; set; }
}
