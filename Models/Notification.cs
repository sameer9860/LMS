using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]  // Links to your custom User table
        public int UserId { get; set; }  // Uses INT to match your User table's PK

        [Required]
        [StringLength(100)]
        public string? Title { get; set; }

        [Required]
        [StringLength(500)]
        public string? Message { get; set; }

        [Required]
        [StringLength(50)]
        public string? NotificationType { get; set; }  // "assignment", "material", "live_class"

        public int RelatedId { get; set; }  // ID of assignment/material/class

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property to your User table
        public virtual User? User { get; set; }

        // Utility fields
        [StringLength(50)]
        public string IconClass { get; set; } = "fas fa-bell";

        [StringLength(255)]
        public string? ActionUrl { get; set; }
    }
}