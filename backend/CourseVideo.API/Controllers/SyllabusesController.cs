using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CourseVideo.API.DTOs.Syllabuses;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/syllabuses")]
[Authorize(Roles = "Admin")]
public class SyllabusesController : ControllerBase
{
    private readonly ISyllabusService _syllabusService;
    private readonly ICourseGenerationService _courseGenerationService;

    public SyllabusesController(ISyllabusService syllabusService, ICourseGenerationService courseGenerationService)
    {
        _syllabusService = syllabusService;
        _courseGenerationService = courseGenerationService;
    }

    [HttpPost("import")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromForm] ImportSyllabusRequest request)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "Vui lòng chọn file đề cương." });
        }

        try
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
            var uploadedByName = User.FindFirstValue(JwtRegisteredClaimNames.Name)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? "Admin";

            var result = await _syllabusService.ImportAsync(request, currentUserId, uploadedByName);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SyllabusListItemResponse>>> GetAll()
    {
        var syllabuses = await _syllabusService.GetAllAsync();
        return Ok(syllabuses);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var syllabus = await _syllabusService.GetByIdAsync(id);
        return syllabus is null ? NotFound() : Ok(syllabus);
    }

    [HttpPost("{id:guid}/generate")]
    public async Task<IActionResult> Generate(Guid id)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
            var currentUserName = User.FindFirstValue(JwtRegisteredClaimNames.Name)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? "Admin";

            var result = await _courseGenerationService.GenerateFromSyllabusAsync(id, currentUserId, currentUserName);
            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("đang chạy"))
        {
            return Conflict(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _syllabusService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
