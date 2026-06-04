using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Category>> GetAllWithCoursesAsync()
    {
        return await _dbContext.Categories
            .Include(category => category.Courses)
            .OrderByDescending(category => category.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Category>> GetVisibleAsync()
    {
        return await _dbContext.Categories
            .Where(category => category.Status == CategoryStatus.Visible)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();
    }

    public Task<Category?> GetByIdAsync(Guid id)
    {
        return _dbContext.Categories
            .Include(category => category.Courses)
            .FirstOrDefaultAsync(category => category.Id == id);
    }

    public async Task<Category?> GetDefaultForAssignmentAsync()
    {
        var aiAndData = await _dbContext.Categories
            .Where(category => category.Status == CategoryStatus.Visible && category.Name == "AI & Data")
            .FirstOrDefaultAsync();

        if (aiAndData is not null)
        {
            return aiAndData;
        }

        return await _dbContext.Categories
            .Where(category => category.Status == CategoryStatus.Visible)
            .OrderBy(category => category.SortOrder)
            .ThenByDescending(category => category.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null)
    {
        var normalizedName = name.Trim().ToLower();

        return _dbContext.Categories.AnyAsync(category =>
            category.Id != excludingId
            && category.Name.ToLower() == normalizedName);
    }

    public Task AddAsync(Category category)
    {
        return _dbContext.Categories.AddAsync(category).AsTask();
    }

    public void Remove(Category category)
    {
        _dbContext.Categories.Remove(category);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
