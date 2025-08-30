using LMS.Models;

namespace LMS.Services
{
   public interface IAIQuizService
{
    Task<List<MCQ>> GenerateMCQsAsync(string textContent);
}
}
