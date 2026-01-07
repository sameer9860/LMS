using System.Threading.Tasks;
using LMS.Models;

namespace LMS.Services
{
    public interface IAIQuizService
    {
        Task<Quiz> GenerateQuizFromMaterialAsync(string textContent, int numberOfQuestions = 10, int courseId = 0);
    }
}
