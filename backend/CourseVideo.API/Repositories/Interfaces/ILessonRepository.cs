using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ILessonRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Lesson> lessons);
    Task<IReadOnlyList<Lesson>> GetByModuleIdAsync(Guid moduleId);
    Task<Lesson?> GetByIdAsync(Guid id);
    Task<Lesson?> GetByIdWithModuleAndCourseAsync(Guid id);
    Task SaveChangesAsync();
}
