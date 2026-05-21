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
}
