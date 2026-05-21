using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.Lessons;

namespace CourseVideo.API.Services.Interfaces;

public interface ILessonService
{
    Task<LessonStructureResponse?> UpdateAsync(Guid id, UpdateLessonRequest request);
    Task<LessonGeneratedContentResponse?> GetGeneratedContentAsync(Guid id);
    Task<LessonGeneratedContentResponse?> UpdateGeneratedContentAsync(Guid id, UpdateLessonGeneratedContentRequest request);
}
