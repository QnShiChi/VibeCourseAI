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

    // Hàm async thì phải trả về Task hoặc Task<T>
    public async Task<IReadOnlyList<Course>> GetAllAsync()
    {
        return await _dbContext.Courses
            .OrderBy(course => course.CreatedAt)
            .ToListAsync();
    }
}
