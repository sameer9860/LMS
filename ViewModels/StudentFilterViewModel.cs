using LMS.Models;
using System.Collections.Generic;

namespace LMS.ViewModels
{
    public class StudentFilterViewModel
    {
        public string? SearchQuery { get; set; }
        public string? Email { get; set; }
        public string? Grade { get; set; }
        public string? GuardianName { get; set; }

        public List<Student>? Students { get; set; }
    }
}
