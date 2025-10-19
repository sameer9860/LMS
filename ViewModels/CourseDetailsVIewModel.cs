using LMS.Models;
using LMS.ViewModels;
namespace LMS.ViewModels
{

    public class CourseDetailsViewModel
    {
        public int Id { get; set; }

        public Course? Course { get; set; }

        public  int CourseId { get; set; }

        public CourseViewModels? EditCourse { get; set; }


        public List<Student>? Students { get; set; }
        public List<Course>? Courses { get; set; }
        public List<Enrollment>? Enrollments { get; set; }
        public List<Material>? Materials { get; set; }
        public List<Assignment>? Assignments { get; set; }
        public List<Instructor>? Instructors { get; set; }

        public List<ChatMessage>? ChatMessages { get; set; }

        public  List<Quiz>? Quizzes { get; set; } // New





        
        public List<LiveClass>? LiveClasses { get; set; }

        public LiveClassViewModel NewLiveClass { get; set; } = new LiveClassViewModel();

    }
}