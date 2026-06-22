using System.Security.Claims;
using CourseVideo.API.Data;
using CourseVideo.API.DTOs.Comments;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/admin/comments")]
[Authorize(Roles = "Admin")]
public class AdminCommentsController : ControllerBase
{
    private readonly ILessonCommentService _lessonCommentService;
    private readonly AppDbContext _dbContext;

    public AdminCommentsController(ILessonCommentService lessonCommentService, AppDbContext dbContext)
    {
        _lessonCommentService = lessonCommentService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminNegativeCommentItemResponse>>> GetComments(
        [FromQuery] string? sentiment = null,
        [FromQuery] string? authorName = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildCommentsQuery(sentiment, authorName);

        if (string.Equals(sentiment, "positive", StringComparison.OrdinalIgnoreCase))
        {
            query = query
                .OrderByDescending(comment => comment.PinnedAt.HasValue)
                .ThenByDescending(comment => comment.PinnedAt)
                .ThenByDescending(comment => comment.CreatedAt);
        }
        else
        {
            query = query.OrderByDescending(comment => comment.CreatedAt);
        }

        return Ok(await query.ToListAsync(cancellationToken));
    }

    [HttpGet("negative")]
    public async Task<ActionResult<IReadOnlyList<AdminNegativeCommentItemResponse>>> GetNegativeComments(CancellationToken cancellationToken = default)
    {
        return await GetComments("negative", null, cancellationToken);
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid commentId, [FromQuery] Guid lessonId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _lessonCommentService.DeleteCommentAsync(
                lessonId,
                commentId,
                GetCurrentUserId(),
                isAdmin: true,
                cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("{commentId:guid}/hide")]
    public async Task<IActionResult> Hide(Guid commentId, [FromQuery] Guid lessonId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _lessonCommentService.HideCommentAsync(lessonId, commentId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("{commentId:guid}/unhide")]
    public async Task<IActionResult> Unhide(Guid commentId, [FromQuery] Guid lessonId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _lessonCommentService.UnhideCommentAsync(lessonId, commentId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPatch("{commentId:guid}/pin")]
    public async Task<IActionResult> PinComment(Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.LessonComments.FirstOrDefaultAsync(item => item.Id == commentId, cancellationToken);

        if (comment == null || comment.DeletedAt != null || comment.IsHidden)
        {
            return NotFound(new { message = "Không tìm thấy bình luận để ghim." });
        }

        if (!string.Equals(comment.Sentiment, "positive", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Chỉ có thể đẩy bình luận tích cực lên trước." });
        }

        comment.PinnedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("positive-courses")]
    public async Task<ActionResult<IReadOnlyList<AdminPositiveCourseHighlightResponse>>> GetPositiveCourseHighlights(CancellationToken cancellationToken = default)
    {
        var comments = await _dbContext.LessonComments
            .AsNoTracking()
            .Where(comment =>
                !comment.IsHidden
                && comment.DeletedAt == null)
            .Select(comment => new
            {
                CourseId = comment.Lesson != null && comment.Lesson.Module != null
                    ? comment.Lesson.Module.CourseId
                    : Guid.Empty,
                CourseTitle = comment.Lesson != null && comment.Lesson.Module != null && comment.Lesson.Module.Course != null
                    ? comment.Lesson.Module.Course.Title
                    : string.Empty,
                comment.Sentiment,
                comment.Content,
                comment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var courses = comments
            .GroupBy(comment => new { comment.CourseId, comment.CourseTitle })
            .Select(group =>
            {
                var positiveComments = group
                    .Where(item => string.Equals(item.Sentiment, "positive", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.CreatedAt)
                    .ToList();
                var latestPositive = positiveComments.FirstOrDefault();
                var totalCommentCount = group.Count();
                var positiveCommentCount = positiveComments.Count;

                return new AdminPositiveCourseHighlightResponse
                {
                    CourseId = group.Key.CourseId,
                    CourseTitle = group.Key.CourseTitle,
                    TotalCommentCount = totalCommentCount,
                    PositiveCommentCount = positiveCommentCount,
                    PositiveRatio = totalCommentCount == 0 ? 0 : (double)positiveCommentCount / totalCommentCount,
                    LatestPositiveCommentContent = latestPositive?.Content ?? string.Empty,
                    LatestPositiveCommentAt = latestPositive?.CreatedAt
                };
            })
            .OrderByDescending(item => item.PositiveRatio)
            .ThenByDescending(item => item.PositiveCommentCount)
            .ThenBy(item => item.CourseTitle)
            .ToList();

        return Ok(courses);
    }

    private IQueryable<AdminNegativeCommentItemResponse> BuildCommentsQuery(string? sentiment, string? authorName)
    {
        var query = _dbContext.LessonComments
            .AsNoTracking()
            .Where(comment => !comment.IsHidden && comment.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(sentiment))
        {
            query = query.Where(comment => comment.Sentiment == sentiment);
        }

        if (!string.IsNullOrWhiteSpace(authorName))
        {
            var normalizedAuthorName = authorName.Trim().ToLower();
            query = query.Where(comment => comment.User != null
                && comment.User.FullName.ToLower().Contains(normalizedAuthorName));
        }

        return query.Select(comment => new AdminNegativeCommentItemResponse
        {
            CommentId = comment.Id,
            LessonId = comment.LessonId,
            LessonTitle = comment.Lesson != null ? comment.Lesson.Title : string.Empty,
            CourseId = comment.Lesson != null && comment.Lesson.Module != null
                ? comment.Lesson.Module.CourseId
                : Guid.Empty,
            CourseTitle = comment.Lesson != null && comment.Lesson.Module != null && comment.Lesson.Module.Course != null
                ? comment.Lesson.Module.Course.Title
                : string.Empty,
            AuthorUserId = comment.UserId,
            AuthorName = comment.User != null ? comment.User.FullName : "Người dùng",
            AuthorEmail = comment.User != null ? comment.User.Email : string.Empty,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            Sentiment = comment.Sentiment ?? string.Empty,
            PinnedAt = comment.PinnedAt
        });
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
    }
}
