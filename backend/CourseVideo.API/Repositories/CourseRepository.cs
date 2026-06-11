using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _dbContext;

    public CourseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Course course)
    {
        return _dbContext.Courses.AddAsync(course).AsTask();
    }

    public async Task<IReadOnlyList<Course>> GetAllAsync()
    {
        return await _dbContext.Courses
            .Include(course => course.Category)
            .OrderBy(course => course.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Course>> GetAdminCoursesAsync()
    {
        return await _dbContext.Courses
            .Include(course => course.Category)
            .Include(course => course.Modules) // We use include here because Category and Module are same level of course and we want to get all the modules of the course, and then we use ThenInclude to get all the lessons of the modules, because we want to show the number of lessons in the admin page.
            .ThenInclude(module => module.Lessons) 
            .OrderByDescending(course => course.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Course>> GetPublishedAsync()
    {
        return await _dbContext.Courses
            .Include(course => course.Category)
            .Include(course => course.Modules)
            .ThenInclude(module => module.Lessons)
            .Where(course => course.IsPublished)
            .OrderByDescending(course => course.CreatedAt)
            .ToListAsync();
    }

    public Task<Course?> GetByIdAsync(Guid id)
    {
        return _dbContext.Courses
            .Include(course => course.Category)
            .FirstOrDefaultAsync(course => course.Id == id);
    }

    public Task<Course?> GetByIdWithStructureAsync(Guid id)
    {
        return _dbContext.Courses
            .Include(course => course.Category)
            .Include(course => course.Modules.OrderBy(module => module.OrderIndex))
            .ThenInclude(module => module.Lessons.OrderBy(lesson => lesson.OrderIndex))
            .Include(course => course.Quizzes)
            .ThenInclude(quiz => quiz.Questions)
            .FirstOrDefaultAsync(course => course.Id == id);
    }

    public Task<int> CountByCategoryIdAsync(Guid categoryId)
    {
        return _dbContext.Courses.CountAsync(course => course.CategoryId == categoryId);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
