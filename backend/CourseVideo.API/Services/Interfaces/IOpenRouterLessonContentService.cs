using CourseVideo.API.Models;
using CourseVideo.API.Models.OpenRouter;

namespace CourseVideo.API.Services.Interfaces;

public interface IOpenRouterLessonContentService
{
    Task<OpenRouterLessonContentResult> GenerateAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken = default);
}
