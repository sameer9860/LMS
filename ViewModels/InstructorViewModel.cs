using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
// Add the correct namespace for User if it exists, e.g.:
using LMS.Models; // <-- Change 'LMS.Models' to the actual namespace where User is defined

public class InstructorViewModel
{
    public int id { get; set; }
    // Personal Info
    [Required]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }  // Optional

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }

    // Account Info
    [Required]
    [Display(Name = "Username")]
    public string? Username { get; set; } // Optional

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Professional Info
    [Required]
    public string Qualification { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Expertise / Subjects")]
    public string Expertise { get; set; } = string.Empty;

    [Display(Name = "Years of Experience")]
    public int? YearsOfExperience { get; set; }

    [Display(Name = "Bio / Short Profile")]
    public string? Bio { get; set; } // Optional

    [Display(Name = "Profile Picture")]
    public IFormFile? ProfileImage { get; set; } // 

    public string? Role { get; set; }
    
      public int UserId { get; set; }
     public User? User { get; set; }
}
