using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/lessons")]
[Authorize(Roles = "Admin")]
public class LessonsController : ControllerBase
{
    private readonly ILessonService _lessonService;

    public LessonsController(ILessonService lessonService)
    {
        _lessonService = lessonService;
    }

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetGeneratedContent(Guid id)
    {
        var content = await _lessonService.GetGeneratedContentAsync(id);
        return content is null ? NotFound() : Ok(content);
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
}
