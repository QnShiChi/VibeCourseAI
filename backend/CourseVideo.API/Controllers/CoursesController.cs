using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.DTOs.Modules;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IModuleService _moduleService;
    private readonly ILessonService _lessonService;

    public CoursesController(ICourseService courseService, IModuleService moduleService, ILessonService lessonService)
    {
        _courseService = courseService;
        _moduleService = moduleService;
        _lessonService = lessonService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<CourseResponse>>> GetAll()
    {
        var courses = await _courseService.GetAllAsync();
        return Ok(courses);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AdminCourseListItemResponse>>> GetAdminCourses()
    {
        var courses = await _courseService.GetAdminCoursesAsync();
        return Ok(courses);
    }

    [HttpGet("published")]
    public async Task<ActionResult<IReadOnlyList<PublishedCourseListItemResponse>>> GetPublishedCourses()
    {
        var courses = await _courseService.GetPublishedCoursesAsync();
        return Ok(courses);
    }

    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var updated = await _courseService.PublishAsync(id);
        return updated is null ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/unpublish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var updated = await _courseService.UnpublishAsync(id);
        return updated is null ? NotFound() : NoContent();
    }

    [HttpGet("{id:guid}/learn")]
    public async Task<IActionResult> GetLearn(Guid id)
    {
        var isAdmin = User.Claims.Any(claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        var payload = await _courseService.GetLearnPayloadAsync(id, isAdmin);
        return payload is null ? NotFound() : Ok(payload);
    }

    [HttpPost("{id:guid}/generate-lesson-content")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateLessonContent(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _courseService.GenerateLessonContentAsync(id, createdByUserId, cancellationToken);
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

    [HttpPost("{courseId:guid}/lessons/{lessonId:guid}/regenerate-lesson-content")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegenerateLessonContent(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _courseService.RegenerateLessonContentAsync(courseId, lessonId, createdByUserId, cancellationToken);
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

    [HttpPost("{id:guid}/generate-lesson-audio")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateLessonAudio(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _courseService.GenerateLessonAudioAsync(id, createdByUserId, cancellationToken);
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

    [HttpPost("{courseId:guid}/lessons/{lessonId:guid}/regenerate-lesson-audio")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegenerateLessonAudio(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _courseService.RegenerateLessonAudioAsync(courseId, lessonId, createdByUserId, cancellationToken);
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

    [HttpPost("{id:guid}/generate-lesson-video")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateLessonVideo(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _courseService.GenerateLessonVideoAsync(id, createdByUserId, cancellationToken);
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

    [HttpPost("{courseId:guid}/lessons/{lessonId:guid}/regenerate-lesson-video")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegenerateLessonVideo(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            var createdByUserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty;
            var response = await _courseService.RegenerateLessonVideoAsync(courseId, lessonId, createdByUserId, cancellationToken);
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

    [HttpGet("{id:guid}/structure")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStructure(Guid id)
    {
        var course = await _courseService.GetStructureAsync(id);
        return course is null ? NotFound() : Ok(course);
    }

    [HttpPut("/api/modules/{id:guid}")]
    public async Task<IActionResult> UpdateModule(Guid id, [FromBody] UpdateModuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { message = "Tiêu đề và mô tả module là bắt buộc." });
        }

        var module = await _moduleService.UpdateAsync(id, request);
        return module is null ? NotFound() : Ok(module);
    }

    [HttpPut("/api/lessons/{id:guid}")]
    public async Task<IActionResult> UpdateLesson(Guid id, [FromBody] UpdateLessonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.ContentSeed))
        {
            return BadRequest(new { message = "Tiêu đề, mô tả và content seed của lesson là bắt buộc." });
        }

        var lesson = await _lessonService.UpdateAsync(id, request);
        return lesson is null ? NotFound() : Ok(lesson);
    }
}
