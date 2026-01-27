using LMS.Models;
using System.Collections.Generic;

namespace LMS.ViewModels
{
    public class InstructorFilterViewModel
    {
        public string? SearchQuery { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Qualification { get; set; }

        public List<Instructor>? Instructors { get; set; }
    }
}
