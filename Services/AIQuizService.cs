using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Linq;

namespace LMS.Services
{
    public class AIQuizService : IAIQuizService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _quizApiKey;

        public AIQuizService(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
            _quizApiKey = config["QuizAPI:ApiKey"] ?? string.Empty;
        }

        /// <summary>
        /// Extracts text content from a PDF file.
        /// </summary>
        public static string ExtractTextFromPdf(string pdfFilePath)
        {
            if (string.IsNullOrWhiteSpace(pdfFilePath) || !File.Exists(pdfFilePath))
                return string.Empty;

            var extractedText = new System.Text.StringBuilder();

            try
            {
                using (var pdfDocument = new PdfDocument(new PdfReader(pdfFilePath)))
                {
                    int pageCount = pdfDocument.GetNumberOfPages();

                    for (int i = 1; i <= pageCount; i++)
                    {
                        var page = pdfDocument.GetPage(i);
                        string pageText = PdfTextExtractor.GetTextFromPage(page);
                        extractedText.Append(pageText).Append(" ");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting PDF text: {ex.Message}");
                return string.Empty;
            }

            return extractedText.ToString().Trim();
        }

        public async Task<Quiz> GenerateQuizFromMaterialAsync(string textContent, int numberOfQuestions = 10, int courseId = 0)
        {
            if (string.IsNullOrWhiteSpace(textContent))
                throw new ArgumentException("textContent cannot be empty.", nameof(textContent));

            var quiz = new Quiz
            {
                Title = "AI-Generated Quiz",
                Type = QuizType.AI,
                CourseId = courseId,
                MCQs = new List<MCQ>()
            };

            try
            {
                // Fetch questions from quizapi.io
                var client = _httpFactory.CreateClient();
                
                string url = $"https://quizapi.io/api/v1/questions?apiKey={Uri.EscapeDataString(_quizApiKey)}&limit={numberOfQuestions}";
                
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Quiz API request failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}");

                var json = await response.Content.ReadAsStringAsync();
                
                // Parse quizapi.io response format
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        var mcq = ParseQuizApiQuestion(item);
                        if (mcq != null)
                            quiz.MCQs.Add(mcq);
                    }
                }

                if (quiz.MCQs.Count == 0)
                    throw new InvalidOperationException("No valid questions were returned from the API.");

                return quiz;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Quiz API request failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a question object from quizapi.io response into an MCQ.
        /// quizapi.io format:
        /// {
        ///   "id": 1,
        ///   "question": "...",
        ///   "description": "...",
        ///   "answers": { "answer_a": "...", "answer_b": "...", ... },
        ///   "correct_answers": { "answer_a_correct": "true", ... },
        ///   "explanation": "..."
        /// }
        /// </summary>
        private static MCQ? ParseQuizApiQuestion(JsonElement element)
        {
            try
            {
                string? question = null;
                if (element.TryGetProperty("question", out var qProp) && qProp.ValueKind == JsonValueKind.String)
                    question = qProp.GetString();

                if (string.IsNullOrWhiteSpace(question))
                    return null;

                // Extract answer options
                var answers = new Dictionary<string, string>();
                if (element.TryGetProperty("answers", out var answersProp) && answersProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in answersProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                            answers[prop.Name] = prop.Value.GetString() ?? "";
                    }
                }

                // Find the correct answer
                string correctAnswer = "";
                if (element.TryGetProperty("correct_answers", out var correctProp) && correctProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in correctProp.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String && prop.Value.GetString() == "true")
                        {
                            // answer_a_correct -> answer_a
                            string keyName = prop.Name.Replace("_correct", "");
                            correctAnswer = keyName;
                            break;
                        }
                    }
                }

                // Map answer_a, answer_b, etc. to OptionA, OptionB, etc.
                string optionA = answers.ContainsKey("answer_a") ? answers["answer_a"] : "";
                string optionB = answers.ContainsKey("answer_b") ? answers["answer_b"] : "";
                string optionC = answers.ContainsKey("answer_c") ? answers["answer_c"] : "";
                string optionD = answers.ContainsKey("answer_d") ? answers["answer_d"] : "";

                // Map answer_a -> A, answer_b -> B, etc.
                string mappedCorrectAnswer = "";
                if (correctAnswer == "answer_a") mappedCorrectAnswer = "A";
                else if (correctAnswer == "answer_b") mappedCorrectAnswer = "B";
                else if (correctAnswer == "answer_c") mappedCorrectAnswer = "C";
                else if (correctAnswer == "answer_d") mappedCorrectAnswer = "D";
                else mappedCorrectAnswer = "A"; // default

                string explanation = "";
                if (element.TryGetProperty("explanation", out var expProp) && expProp.ValueKind == JsonValueKind.String)
                    explanation = expProp.GetString() ?? "";

                return new MCQ
                {
                    Question = question,
                    OptionA = optionA,
                    OptionB = optionB,
                    OptionC = optionC,
                    OptionD = optionD,
                    CorrectAnswer = mappedCorrectAnswer,
                    Feedback = explanation
                };
            }
            catch
            {
                return null;
            }
        }

        // DTO for JSON deserialization (kept for backward compatibility if needed)
        private class MCQDto
        {
            public string? Question { get; set; }
            public string? OptionA { get; set; }
            public string? OptionB { get; set; }
            public string? OptionC { get; set; }
            public string? OptionD { get; set; }
            public string? CorrectAnswer { get; set; }
            public string? Feedback { get; set; }
        }
    }
}
