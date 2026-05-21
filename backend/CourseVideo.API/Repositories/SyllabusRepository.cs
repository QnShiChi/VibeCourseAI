using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class SyllabusRepository : ISyllabusRepository
{
    private readonly AppDbContext _dbContext;

    public SyllabusRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Syllabus syllabus)
    {
        return _dbContext.Syllabuses.AddAsync(syllabus).AsTask();
    }

    public async Task<IReadOnlyList<Syllabus>> GetAllAsync()
    {
        return await _dbContext.Syllabuses
            .Include(syllabus => syllabus.UploadedByUser)
            .OrderByDescending(syllabus => syllabus.CreatedAt)
            .ToListAsync();
    }

    public Task<Syllabus?> GetByIdAsync(Guid id)
    {
        return _dbContext.Syllabuses
            .Include(syllabus => syllabus.UploadedByUser)
            .FirstOrDefaultAsync(syllabus => syllabus.Id == id);
    }

    public Task<Syllabus?> GetEntityByIdAsync(Guid id)
    {
        return _dbContext.Syllabuses.FirstOrDefaultAsync(syllabus => syllabus.Id == id);
    }

    public Task DeleteAsync(Syllabus syllabus)
    {
        _dbContext.Syllabuses.Remove(syllabus);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
