using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IModuleRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Module> modules);
    Task<IReadOnlyList<Module>> GetByCourseIdAsync(Guid courseId);
    Task<Module?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
}
