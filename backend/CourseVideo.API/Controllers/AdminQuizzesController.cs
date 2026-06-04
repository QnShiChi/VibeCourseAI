using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/admin/quizzes")]
[Authorize(Roles = "Admin")]
public class AdminQuizzesController : ControllerBase
{
    private readonly IQuizGenerationService _quizGenerationService;

    public AdminQuizzesController(IQuizGenerationService quizGenerationService)
    {
        _quizGenerationService = quizGenerationService;
    }

    [HttpPost("{quizId:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid quizId, CancellationToken cancellationToken = default)
    {
        await _quizGenerationService.RegenerateQuizAsync(quizId, cancellationToken);
        return Accepted();
    }
}
