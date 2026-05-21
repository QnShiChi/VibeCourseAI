using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly AppDbContext _dbContext;

    public ModuleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddRangeAsync(IReadOnlyCollection<Module> modules)
    {
        return _dbContext.Modules.AddRangeAsync(modules);
    }

    public async Task<IReadOnlyList<Module>> GetByCourseIdAsync(Guid courseId)
    {
        return await _dbContext.Modules
            .Include(module => module.Lessons.OrderBy(lesson => lesson.OrderIndex))
            .Where(module => module.CourseId == courseId)
            .OrderBy(module => module.OrderIndex)
            .ToListAsync();
    }

    public Task<Module?> GetByIdAsync(Guid id)
    {
        return _dbContext.Modules
            .Include(module => module.Lessons.OrderBy(lesson => lesson.OrderIndex))
            .FirstOrDefaultAsync(module => module.Id == id);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
