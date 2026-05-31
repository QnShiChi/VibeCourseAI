using CourseVideo.API.DTOs.GenerationJobs;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/generation-jobs")]
[Authorize(Roles = "Admin")]
public class GenerationJobsController : ControllerBase
{
    private readonly ICourseGenerationService _courseGenerationService;

    public GenerationJobsController(ICourseGenerationService courseGenerationService)
    {
        _courseGenerationService = courseGenerationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GenerationJobListItemResponse>>> GetAll()
    {
        var jobs = await _courseGenerationService.GetAllJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var job = await _courseGenerationService.GetJobByIdAsync(id);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelJob(Guid id)
    {
        try
        {
            await _courseGenerationService.CancelJobAsync(id);
            return Ok(new { Message = "Đã hủy tiến trình thành công." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
