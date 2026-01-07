using System.ComponentModel.DataAnnotations;

namespace LMS.Models;

public class User
{
  [Key]
  public int Id { get; set; }
  [Required]
  public string? Username { get; set; }
  [Required]
  public string? Password { get; set; }
  [Required]
  public string? Role { get; set; }  // e.g., "Admin", "Instructor", "Student"

   // New property to track who created this user
  
                          
  

}
