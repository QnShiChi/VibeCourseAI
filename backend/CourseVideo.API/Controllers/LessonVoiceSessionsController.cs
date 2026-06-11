using System.Security.Claims;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class LessonVoiceSessionsController : ControllerBase
{
    private readonly ILessonVoiceTutorSessionService _sessionService;

    public LessonVoiceSessionsController(ILessonVoiceTutorSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("lessons/{lessonId:guid}/voice-sessions")]
    public async Task<IActionResult> CreateSession(Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionService.CreateOrResumeSessionAsync(
                lessonId,
                GetCurrentUserId(),
                User.IsInRole("Admin"),
                cancellationToken);

            return Ok(session);
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

    [HttpGet("lessons/{lessonId:guid}/voice-sessions/current")]
    public async Task<IActionResult> GetCurrentSession(Guid lessonId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetCurrentSessionAsync(lessonId, GetCurrentUserId(), cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("voice-sessions/{sessionId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var messages = await _sessionService.GetMessagesAsync(sessionId, GetCurrentUserId(), cancellationToken);
            return Ok(messages);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("voice-sessions/{sessionId:guid}/close")]
    public async Task<IActionResult> CloseSession(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await _sessionService.CloseSessionAsync(sessionId, GetCurrentUserId(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
    }
}
