using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/lessons")]
[Authorize(Roles = "Admin")]
public class LessonsController : ControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ILessonAudioGenerationService _lessonAudioGenerationService;
    private readonly ILessonVideoGenerationService _lessonVideoGenerationService;

    public LessonsController(
        ILessonService lessonService,
        ILessonAudioGenerationService lessonAudioGenerationService,
        ILessonVideoGenerationService lessonVideoGenerationService)
    {
        _lessonService = lessonService;
        _lessonAudioGenerationService = lessonAudioGenerationService;
        _lessonVideoGenerationService = lessonVideoGenerationService;
    }

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetGeneratedContent(Guid id)
    {
        var content = await _lessonService.GetGeneratedContentAsync(id);
        return content is null ? NotFound() : Ok(content);
    }

    [HttpGet("{id:guid}/audio")]
    public async Task<IActionResult> GetAudio(Guid id)
    {
        var audio = await _lessonService.GetAudioAsync(id);
        return audio is null ? NotFound() : Ok(audio);
    }

    [HttpGet("{id:guid}/video")]
    public async Task<IActionResult> GetVideo(Guid id)
    {
        var video = await _lessonService.GetVideoAsync(id);
        return video is null ? NotFound() : Ok(video);
    }

    [HttpPut("{id:guid}/content")]
    public async Task<IActionResult> UpdateGeneratedContent(Guid id, [FromBody] UpdateLessonGeneratedContentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TeachingScript) ||
            string.IsNullOrWhiteSpace(request.SlideOutlineJson) ||
            string.IsNullOrWhiteSpace(request.VoiceoverPlanJson))
        {
            return BadRequest(new { message = "Script, slide outline va voiceover plan la bat buoc." });
        }

        try
        {
            var content = await _lessonService.UpdateGeneratedContentAsync(id, request);
            return content is null ? NotFound() : Ok(content);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/generate-audio")]
    public async Task<IActionResult> GenerateAudio(Guid id, [FromQuery] Guid courseId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _lessonAudioGenerationService.GenerateLessonAudioAsync(courseId, id, createdByUserId, cancellationToken);
            return Ok(response);
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

    [HttpPost("{id:guid}/generate-video")]
    public async Task<IActionResult> GenerateVideo(Guid id, [FromQuery] Guid courseId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _lessonVideoGenerationService.GenerateLessonVideoAsync(courseId, id, createdByUserId, cancellationToken);
            return Ok(response);
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
