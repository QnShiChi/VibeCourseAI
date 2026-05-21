using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ICourseRepository
{
    Task AddAsync(Course course);
    Task<IReadOnlyList<Course>> GetAllAsync();
    Task<IReadOnlyList<Course>> GetAdminCoursesAsync();
    Task<IReadOnlyList<Course>> GetPublishedAsync();
    Task<Course?> GetByIdAsync(Guid id);
    Task<Course?> GetByIdWithStructureAsync(Guid id);
    Task SaveChangesAsync();
}
