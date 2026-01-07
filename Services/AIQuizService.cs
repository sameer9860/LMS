using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models;
using OA = OpenAI.Chat;

namespace LMS.Services
{
    public class AIQuizService : IAIQuizService
    {
        private readonly OA.ChatClient _chatClient;

        public AIQuizService(OA.ChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
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

            string prompt =
$@"Generate {numberOfQuestions} multiple-choice questions from the following text.
Return the result strictly as a JSON array (no extra commentary). 
Each item must have these fields: 
Question, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, Feedback.

Text:
---
{textContent}
---";

            // Use OA alias for all OpenAI.Chat types
            var messages = new List<OA.ChatMessage>
            {
                new OA.SystemChatMessage("You are a helpful quiz generator. Return only a JSON array of questions."),
                new OA.UserChatMessage(prompt)
            };

            OA.ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);

            string aiOutput = completion?.Content?.Count > 0 ? completion.Content[0].Text ?? string.Empty : string.Empty;

            string json = ExtractJson(aiOutput);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var parsed = JsonSerializer.Deserialize<List<MCQDto>>(json, options);
                if (parsed != null)
                {
                    foreach (var dto in parsed)
                    {
                        quiz.MCQs.Add(new MCQ
                        {
                            Question = dto.Question ?? "",
                            OptionA = dto.OptionA ?? "",
                            OptionB = dto.OptionB ?? "",
                            OptionC = dto.OptionC ?? "",
                            OptionD = dto.OptionD ?? "",
                            CorrectAnswer = dto.CorrectAnswer ?? "",
                            Feedback = dto.Feedback ?? ""
                        });
                    }
                }
                else
                {
                    quiz.MCQs.Add(CreateFallbackMCQ(aiOutput));
                }
            }
            catch (JsonException)
            {
                quiz.MCQs.Add(CreateFallbackMCQ(aiOutput));
            }

            return quiz;
        }

        // Extract JSON from AI output (removes Markdown code fences etc.)
        private static string ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "[]";

            var trimmed = text.Trim();
            if (trimmed.StartsWith("```"))
            {
                int startFence = trimmed.IndexOf("```");
                int endFence = trimmed.LastIndexOf("```");
                if (endFence > startFence)
                {
                    var inner = trimmed.Substring(startFence + 3, endFence - (startFence + 3));
                    if (inner.TrimStart().StartsWith("json", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = inner.IndexOf('\n');
                        if (idx >= 0) inner = inner.Substring(idx + 1);
                    }
                    return inner.Trim();
                }
            }

            int arrStart = trimmed.IndexOf('[');
            int arrEnd = trimmed.LastIndexOf(']');
            if (arrStart >= 0 && arrEnd > arrStart)
                return trimmed.Substring(arrStart, arrEnd - arrStart + 1);

            return trimmed;
        }

        private static MCQ CreateFallbackMCQ(string text)
        {
            return new MCQ
            {
                Question = "AI output (parse failed)",
                OptionA = text.Length > 200 ? text.Substring(0, 200) + "..." : text,
                OptionB = "N/A",
                OptionC = "N/A",
                OptionD = "N/A",
                CorrectAnswer = "A",
                Feedback = "The AI returned non-JSON or parsing failed; check raw output in OptionA."
            };
        }

        // DTO for JSON deserialization
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