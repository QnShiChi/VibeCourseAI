using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ISyllabusRepository
{
    Task AddAsync(Syllabus syllabus);
    Task<IReadOnlyList<Syllabus>> GetAllAsync();
    Task<Syllabus?> GetByIdAsync(Guid id);
    Task<Syllabus?> GetEntityByIdAsync(Guid id);
    Task DeleteAsync(Syllabus syllabus);
    Task SaveChangesAsync();
}
