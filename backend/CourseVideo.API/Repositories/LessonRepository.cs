using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class LessonRepository : ILessonRepository
{
    private readonly AppDbContext _dbContext;

    public LessonRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddRangeAsync(IReadOnlyCollection<Lesson> lessons)
    {
        return _dbContext.Lessons.AddRangeAsync(lessons);
    }

    public async Task<IReadOnlyList<Lesson>> GetByModuleIdAsync(Guid moduleId)
    {
        return await _dbContext.Lessons
            .Where(lesson => lesson.ModuleId == moduleId)
            .OrderBy(lesson => lesson.OrderIndex)
            .ToListAsync();
    }

    public Task<Lesson?> GetByIdAsync(Guid id)
    {
        return _dbContext.Lessons.FirstOrDefaultAsync(lesson => lesson.Id == id);
    }

    public Task<Lesson?> GetByIdWithModuleAndCourseAsync(Guid id)
    {
        return _dbContext.Lessons
            .Include(lesson => lesson.Module!)
            .ThenInclude(module => module.Course)
            .FirstOrDefaultAsync(lesson => lesson.Id == id);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
