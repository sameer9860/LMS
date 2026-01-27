using Microsoft.EntityFrameworkCore;
using LMS.Models;

namespace LMS.Views.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<LiveClass> LiveClasses { get; set; }

        // New DbSet for Notifications
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<Quiz> Quizzes { get; set; }

        public DbSet<MCQ> MCQs { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }

        public DbSet<QuizSubmission> QuizSubmissions { get; set; }

        public DbSet<QuizAnswer> QuizAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActivityLog>()
                .Property(e => e.ActivityType)
                .HasConversion<int>();

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => new { a.UserId, a.Timestamp });

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => new { a.CourseId, a.Timestamp });

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => new { a.ActivityType, a.Timestamp });

            // Configure cascade delete for QuizAnswer
            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.Submission)
                .WithMany(qs => qs.Answers)
                .HasForeignKey(qa => qa.QuizSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
