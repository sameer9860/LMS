using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using LMS.Models;


public class Instructor
{

  [Key]
  public int id { get; set; }

  public int UserId { get; set; }
  public User? User { get; set; }
  // Personal Info
  [Required]
  [Display(Name = "First Name")]
  public string? FirstName { get; set; }

  [Display(Name = "Middle Name")]
  public string? MiddleName { get; set; }  // Optional

  [Required]
  [Display(Name = "Last Name")]
  public string? LastName { get; set; }

  [Required]
  [EmailAddress]
  [Display(Name = "Email Address")]
  public string? Email { get; set; }

  [Required]
  [Display(Name = "Phone Number")]
  public string? PhoneNumber { get; set; }

  [Required]
  public string? Gender { get; set; }

  [Required]
  [DataType(DataType.Date)]
  [Display(Name = "Date of Birth")]
  public DateTime DateOfBirth { get; set; }

  //  // Account Info
  //   [Required]
  //   [Display(Name = "Username")]
  //   public string? Username { get; set; } // Optional

  //   [Required]
  //   [DataType(DataType.Password)]
  //   public string? Password { get; set; }


  // Professional Info
  [Required]
  public string? Qualification { get; set; }

  [Required]
  [Display(Name = "Expertise / Subjects")]
  public string? Expertise { get; set; }

  [Display(Name = "Years of Experience")]
  public int? YearsOfExperience { get; set; }

  [Display(Name = "Bio / Short Profile")]
  public string? Bio { get; set; } // Optional

  [Display(Name = "Profile Picture")]
  public string? ProfileImagePath { get; set; } // Optional
    
   public virtual ICollection<Course>? Courses { get; set; }
}
