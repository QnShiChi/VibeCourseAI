using System.Security.Claims;
using CourseVideo.API.DTOs.Quizzes;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet("lessons/{lessonId:guid}/quiz")]
    public async Task<ActionResult<QuizResponse>> GetLessonQuiz(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.Claims.Any(claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        var quiz = await _quizService.GetLessonQuizAsync(lessonId, userId, isAdmin, cancellationToken);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpGet("courses/{courseId:guid}/final-quiz")]
    public async Task<ActionResult<QuizResponse>> GetFinalQuiz(Guid courseId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.Claims.Any(claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        var quiz = await _quizService.GetFinalQuizAsync(courseId, userId, isAdmin, cancellationToken);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpPost("quizzes/{quizId:guid}/attempts")]
    public async Task<ActionResult<CreateQuizAttemptResponse>> StartAttempt(Guid quizId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _quizService.StartAttemptAsync(quizId, userId, cancellationToken));
    }

    [HttpPost("quizzes/{quizId:guid}/attempts/{attemptId:guid}/submit")]
    public async Task<ActionResult<SubmitQuizAttemptResponse>> SubmitAttempt(Guid quizId, Guid attemptId, [FromBody] SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _quizService.SubmitAttemptAsync(quizId, attemptId, userId, request, cancellationToken));
    }

    [HttpGet("quizzes/{quizId:guid}/attempts")]
    public async Task<ActionResult<IReadOnlyList<QuizAttemptHistoryItemResponse>>> GetAttemptHistory(Guid quizId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _quizService.GetAttemptHistoryAsync(quizId, userId, cancellationToken));
    }
}
