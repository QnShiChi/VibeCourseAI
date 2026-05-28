using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;

namespace CourseVideo.API.Services.Interfaces;

public interface IOpenRouterQuizGenerationService
{
    Task<OpenRouterQuizGenerationResult> GenerateLessonQuizAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken = default);
    Task<OpenRouterQuizGenerationResult> GenerateFinalQuizAsync(Course course, IReadOnlyList<Lesson> lessons, CancellationToken cancellationToken = default);
}
