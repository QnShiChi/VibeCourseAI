using Microsoft.AspNetCore.Mvc;
using CourseVideo.API.DTOs.AudioWorker;
using CourseVideo.API.Services.Audio;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("")]
public class AudioWorkerJobsController : ControllerBase
{
    private readonly INarrationService _narrationService;
    private readonly IAudioPipelineService _audioPipelineService;
    private readonly ILogger<AudioWorkerJobsController> _logger;

    public AudioWorkerJobsController(
        INarrationService narrationService,
        IAudioPipelineService audioPipelineService,
        ILogger<AudioWorkerJobsController> logger)
    {
        _narrationService = narrationService;
        _audioPipelineService = audioPipelineService;
        _logger = logger;
    }

    [HttpPost("/jobs/generate-lesson-audio")]
    public async Task<ActionResult<AudioWorkerLessonResponse>> GenerateLessonAudio([FromBody] AudioWorkerLessonRequest request)
    {
        try
        {
            var segments = _narrationService.BuildNarrationSegments(
                request.TeachingScript,
                request.SlideOutlineJson,
                request.VoiceoverPlanJson
            );

            if (segments.Count == 0)
            {
                return BadRequest("Lesson phải có ít nhất một slide để render audio.");
            }

            var response = await _audioPipelineService.GenerateLessonAudioAsync(request.LessonId, segments);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Lỗi dữ liệu đầu vào khi xử lý job generate-lesson-audio");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý job generate-lesson-audio");
            return BadRequest(ex.Message);
        }
    }
}
