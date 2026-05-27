using System.Diagnostics;
using CourseVideo.API.DTOs.AudioWorker;
using CourseVideo.API.Services.Video;

namespace CourseVideo.API.Services.Audio;

public class AudioPipelineService : IAudioPipelineService
{
    private readonly IEdgeTtsService _edgeTtsService;
    private readonly IStorageService _storageService;
    private readonly ILogger<AudioPipelineService> _logger;

    public AudioPipelineService(IEdgeTtsService edgeTtsService, IStorageService storageService, ILogger<AudioPipelineService> logger)
    {
        _edgeTtsService = edgeTtsService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<AudioWorkerLessonResponse> GenerateLessonAudioAsync(Guid lessonId, List<NarrationSegment> narrationSegments, CancellationToken cancellationToken = default)
    {
        var results = new List<AudioWorkerSegmentResponse>();
        var audioPaths = new List<string>();

        var storageDir = _storageService.GetStorageDirectory();
        var audioDir = Path.Combine(storageDir, "audio");
        if (!Directory.Exists(audioDir))
        {
            Directory.CreateDirectory(audioDir);
        }

        foreach (var segment in narrationSegments)
        {
            _logger.LogInformation($"Generating audio for slide {segment.SlideNumber}: '{segment.NarrationText}'");
            var audioBytes = await _edgeTtsService.SynthesizeToBytesAsync(segment.NarrationText, cancellationToken: cancellationToken);
            
            var fileName = $"{lessonId}-slide-{segment.SlideNumber}.mp3";
            var filePath = Path.Combine(audioDir, fileName);
            await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);
            
            var duration = await GetAudioDurationAsync(filePath, cancellationToken);
            
            results.Add(new AudioWorkerSegmentResponse
            {
                SlideNumber = segment.SlideNumber,
                Title = segment.Title,
                NarrationText = segment.NarrationText,
                AudioUrl = $"/storage/audio/{fileName}",
                DurationSeconds = duration
            });

            audioPaths.Add(filePath);
            await Task.Delay(1500, cancellationToken);
        }

        results = results.OrderBy(r => r.SlideNumber).ToList();
        
        var finalFileName = $"{lessonId}.mp3";
        var finalFilePath = Path.Combine(audioDir, finalFileName);
        
        await ConcatenateAudioFilesAsync(audioPaths, finalFilePath, cancellationToken);
        var finalDuration = await GetAudioDurationAsync(finalFilePath, cancellationToken);

        return new AudioWorkerLessonResponse
        {
            AudioUrl = $"/storage/audio/{finalFileName}",
            DurationSeconds = finalDuration,
            Segments = results,
            ErrorMessage = null
        };
    }

    private async Task ConcatenateAudioFilesAsync(List<string> filePaths, string outputPath, CancellationToken cancellationToken)
    {
        if (filePaths.Count == 0)
        {
            throw new ArgumentException("Không có segment audio để ghép.");
        }

        using var destinationStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        foreach (var path in filePaths)
        {
            using var sourceStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }
    }

    private async Task<double> GetAudioDurationAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null) return 0.0;

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && double.TryParse(output.Trim(), System.Globalization.CultureInfo.InvariantCulture, out double duration))
            {
                return duration;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Could not get duration for {filePath}");
        }

        return 0.0;
    }
}
