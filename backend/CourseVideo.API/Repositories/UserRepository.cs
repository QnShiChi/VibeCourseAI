using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _dbContext.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email == email);
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _dbContext.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await _dbContext.Users
            .Include(user => user.Role)
            .OrderBy(user => user.CreatedAt)
            .ToListAsync();
    }

    public Task AddAsync(User user)
    {
        return _dbContext.Users.AddAsync(user).AsTask();
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
