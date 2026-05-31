using Microsoft.AspNetCore.Mvc;
using CourseVideo.API.DTOs.VideoWorker;
using CourseVideo.API.Services.Video;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("")]
public class VideoWorkerJobsController : ControllerBase
{
    private readonly ITimelineService _timelineService;
    private readonly IStorageService _storageService;
    private readonly IRenderService _renderService;
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<VideoWorkerJobsController> _logger;

    public VideoWorkerJobsController(
        ITimelineService timelineService,
        IStorageService storageService,
        IRenderService renderService,
        IFFmpegService ffmpegService,
        ILogger<VideoWorkerJobsController> logger)
    {
        _timelineService = timelineService;
        _storageService = storageService;
        _renderService = renderService;
        _ffmpegService = ffmpegService;
        _logger = logger;
    }

    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpPost("/jobs/generate-lesson-video")]
    public async Task<ActionResult<VideoWorkerLessonResponse>> GenerateLessonVideo([FromBody] VideoWorkerLessonRequest request)
    {
        try
        {
            var slides = _timelineService.ParseSlideOutlineJson(request.SlideOutlineJson);
            if (slides.Count == 0)
            {
                return BadRequest("Lesson phải có ít nhất một slide để render video.");
            }

            var audioSegments = _timelineService.ParseAudioSegmentsJson(request.AudioSegmentsJson);
            var timeline = _timelineService.BuildSlideTimeline(audioSegments);

            var slideLookup = slides.ToDictionary(s => s.SlideNumber, s => s);
            var slidePaths = new List<string>();
            var durations = new List<double>();
            
            var framesDir = _storageService.BuildVideoFramesDir(request.LessonId);

            foreach (var item in timeline)
            {
                if (!slideLookup.TryGetValue(item.SlideNumber, out var slide))
                {
                    return BadRequest($"Không tìm thấy slide {item.SlideNumber} để khớp với audio segment.");
                }

                var slidePath = Path.Combine(framesDir, $"slide-{item.SlideNumber:D3}.png");
                await _renderService.RenderSlidePngAsync(slidePath, slide);
                
                slidePaths.Add(slidePath);
                durations.Add(item.DurationSeconds);
            }

            var audioPath = _storageService.ResolveStoragePathFromUrl(request.AudioUrl);
            var finalPath = _storageService.BuildVideoOutputPath(request.LessonId);
            
            var totalDuration = await _ffmpegService.AssembleVideoAsync(slidePaths, durations, audioPath, finalPath);

            return Ok(new VideoWorkerLessonResponse
            {
                VideoUrl = $"/storage/video/{Path.GetFileName(finalPath)}",
                DurationSeconds = totalDuration,
                SlideTimings = timeline
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý job generate-lesson-video");
            return BadRequest(ex.Message);
        }
    }
}
