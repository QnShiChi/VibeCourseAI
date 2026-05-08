using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
