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

    [HttpGet("payment-overview")]
    public async Task<IActionResult> GetPaymentOverview()
    {
        var orders = await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.Course)
            .ToListAsync();
        var visibleOrders = orders
            .Where(order => order.Status != "Pending")
            .ToList();
        var today = DateTime.UtcNow.Date;
        var timeline = Enumerable.Range(0, 7)
            .Select(index => today.AddDays(-(6 - index)))
            .Select(date => new DashboardPaymentTimelinePointResponse
            {
                Date = date,
                Label = date.ToString("dd/MM"),
                PaidOrders = visibleOrders.Count(order =>
                    (order.Status == "Paid" || order.Status == "LatePaid")
                    && (order.PaidAt ?? order.CreatedAt).Date == date),
                PendingOrders = 0,
                FailedOrExpiredOrders = visibleOrders.Count(order =>
                    (order.Status == "Expired" || order.Status == "Failed" || order.Status == "Cancelled")
                    && order.CreatedAt.Date == date)
            })
            .ToList();

        var recentOrders = visibleOrders
            .OrderByDescending(order => order.PaidAt ?? order.CreatedAt)
            .Take(8)
            .Select(order => new DashboardRecentPaymentOrderItemResponse
            {
                PaymentOrderId = order.Id,
                OrderCode = order.OrderCode,
                UserId = order.UserId,
                UserFullName = order.User?.FullName ?? "Người dùng",
                UserEmail = order.User?.Email ?? string.Empty,
                CourseId = order.CourseId,
                CourseTitle = order.Course?.Title ?? string.Empty,
                Amount = order.Amount,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt
            })
            .ToList();

        return Ok(new DashboardPaymentOverviewResponse
        {
            TotalOrders = visibleOrders.Count,
            PaidOrders = visibleOrders.Count(order => order.Status == "Paid" || order.Status == "LatePaid"),
            PendingOrders = 0,
            FailedOrExpiredOrders = visibleOrders.Count(order =>
                order.Status == "Expired" || order.Status == "Failed" || order.Status == "Cancelled"),
            Timeline = timeline,
            RecentOrders = recentOrders
        });
    }
}
