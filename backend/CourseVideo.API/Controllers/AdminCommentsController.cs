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

    [HttpGet("negative")]
    public async Task<ActionResult<IReadOnlyList<AdminNegativeCommentItemResponse>>> GetNegativeComments(CancellationToken cancellationToken = default)
    {
        var comments = await _dbContext.LessonComments
            .AsNoTracking()
            .Where(comment =>
                comment.Sentiment == "negative"
                && !comment.IsHidden
                && comment.DeletedAt == null)
            .OrderByDescending(comment => comment.CreatedAt)
            .Select(comment => new AdminNegativeCommentItemResponse
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
                Sentiment = comment.Sentiment ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return Ok(comments);
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

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
    }
}
