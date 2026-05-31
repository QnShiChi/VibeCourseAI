using CourseVideo.API.DTOs.VideoWorker;

namespace CourseVideo.API.Services.Video;

public interface IStorageService
{
    string BuildVideoOutputPath(string lessonId);
    string BuildVideoFramesDir(string lessonId);
    string ResolveStoragePathFromUrl(string storageUrl);
    string GetStorageDirectory();
}

public interface ITimelineService
{
    List<AudioSegment> ParseAudioSegmentsJson(string json);
    List<SlideItem> ParseSlideOutlineJson(string json);
    List<SlideTimingResponse> BuildSlideTimeline(List<AudioSegment> audioSegments);
}

public interface IImageProvider
{
    Task<byte[]?> FetchImageForSlideAsync(string keyword, CancellationToken cancellationToken = default);
}

public interface IRenderService
{
    Task RenderSlidePngAsync(string outputPath, SlideItem slide, CancellationToken cancellationToken = default);
}

public interface IFFmpegService
{
    Task<double> AssembleVideoAsync(
        List<string> slidePaths,
        List<double> durations,
        string audioPath,
        string outputPath,
        CancellationToken cancellationToken = default);
}
