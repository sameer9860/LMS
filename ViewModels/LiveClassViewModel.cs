using System;
using System.ComponentModel.DataAnnotations;

namespace LMS.ViewModels
{
    public class LiveClassViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string? Title { get; set; }

        
        public string? RoomName { get; set; }  // This is like a virtual meeting room ID

        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        public string? Description { get; set; }

        [Required]
        public int CourseId { get; set; }


        public int Instructorid { get; set; } // Link to the instructor

        // These are optional, but useful if needed later
        public bool IsLive { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
    }
}
