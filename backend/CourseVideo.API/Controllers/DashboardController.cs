using CourseVideo.API.Data;
using CourseVideo.API.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public DashboardController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var usersCount = await _dbContext.Users.CountAsync();
        var syllabusesCount = await _dbContext.Syllabuses.CountAsync();
        var coursesCount = await _dbContext.Courses.CountAsync();
        var generationJobsCount = await _dbContext.GenerationJobs.CountAsync();

        var negativeCommentsCount = await _dbContext.LessonComments
            .AsNoTracking()
            .Where(comment =>
                comment.Sentiment == "negative"
                && !comment.IsHidden
                && comment.DeletedAt == null)
            .CountAsync();

        return Ok(new DashboardStatsResponse
        {
            UsersCount = usersCount,
            SyllabusesCount = syllabusesCount,
            CoursesCount = coursesCount,
            GenerationJobsCount = generationJobsCount,
            NegativeCommentsCount = negativeCommentsCount
        });
    }
}
