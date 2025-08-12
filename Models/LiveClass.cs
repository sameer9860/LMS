using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.Models
{
    public class LiveClass
    {
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

    
        public string? RoomName { get; set; }  // Jitsi room

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? Description { get; set; }

        public bool IsLive { get; set; } = false; // true = in progress

        public bool IsCompleted { get; set; }  = false; // true = completed

        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }
    }
}
