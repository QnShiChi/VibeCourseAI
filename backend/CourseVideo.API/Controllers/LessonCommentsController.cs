using System.Security.Claims;
using CourseVideo.API.DTOs.Comments;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/lessons/{lessonId:guid}/comments")]
[Authorize]
public class LessonCommentsController : ControllerBase
{
    private readonly ILessonCommentService _lessonCommentService;

    public LessonCommentsController(ILessonCommentService lessonCommentService)
    {
        _lessonCommentService = lessonCommentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(
        Guid lessonId,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _lessonCommentService.GetCommentsAsync(
                lessonId,
                GetCurrentUserId(),
                User.IsInRole("Admin"),
                sort,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
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

    [HttpPost]
    public async Task<IActionResult> CreateComment(Guid lessonId, [FromBody] CreateLessonCommentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _lessonCommentService.CreateCommentAsync(
                lessonId,
                GetCurrentUserId(),
                User.IsInRole("Admin"),
                request,
                cancellationToken);

            return Ok(created);
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

    [HttpPost("{commentId:guid}/replies")]
    public async Task<IActionResult> CreateReply(Guid lessonId, Guid commentId, [FromBody] CreateLessonReplyRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _lessonCommentService.CreateReplyAsync(
                lessonId,
                commentId,
                GetCurrentUserId(),
                User.IsInRole("Admin"),
                request,
                cancellationToken);

            return Ok(created);
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

    [HttpPost("{commentId:guid}/reactions")]
    public async Task<IActionResult> AddReaction(Guid lessonId, Guid commentId, [FromBody] ToggleLessonCommentReactionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _lessonCommentService.AddReactionAsync(
                lessonId,
                commentId,
                GetCurrentUserId(),
                User.IsInRole("Admin"),
                request.Emoji,
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

    [HttpDelete("{commentId:guid}/reactions/{emoji}")]
    public async Task<IActionResult> RemoveReaction(Guid lessonId, Guid commentId, string emoji, CancellationToken cancellationToken = default)
    {
        try
        {
            await _lessonCommentService.RemoveReactionAsync(
                lessonId,
                commentId,
                GetCurrentUserId(),
                User.IsInRole("Admin"),
                emoji,
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

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _lessonCommentService.DeleteCommentAsync(
                lessonId,
                commentId,
                GetCurrentUserId(),
                User.IsInRole("Admin"),
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

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
    }
}
