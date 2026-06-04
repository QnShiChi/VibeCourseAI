using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllWithCoursesAsync();
    Task<IReadOnlyList<Category>> GetVisibleAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category?> GetDefaultForAssignmentAsync();
    Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null);
    Task AddAsync(Category category);
    void Remove(Category category);
    Task SaveChangesAsync();
}
