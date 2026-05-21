using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/admin/comments")]
[Authorize(Roles = "Admin")]
public class AdminCommentsController : ControllerBase
{
    private readonly ILessonCommentService _lessonCommentService;

    public AdminCommentsController(ILessonCommentService lessonCommentService)
    {
        _lessonCommentService = lessonCommentService;
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
}
