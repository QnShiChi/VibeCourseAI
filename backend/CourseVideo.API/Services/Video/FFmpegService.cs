using System.Diagnostics;

namespace CourseVideo.API.Services.Video;

public class FFmpegService : IFFmpegService
{
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(ILogger<FFmpegService> logger)
    {
        _logger = logger;
    }

    public async Task<double> AssembleVideoAsync(
        List<string> slidePaths,
        List<double> durations,
        string audioPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (slidePaths == null || slidePaths.Count == 0)
            throw new ArgumentException("Không có slide để render video.");
            
        if (slidePaths.Count != durations.Count)
            throw new ArgumentException("Số lượng slide và durations không khớp.");

        if (!File.Exists(audioPath))
            throw new FileNotFoundException($"Không tìm thấy audio source: {audioPath}");

        var manifestPath = Path.ChangeExtension(outputPath, ".txt");
        var manifestLines = new List<string>();

        for (int i = 0; i < slidePaths.Count; i++)
        {
            // Escape single quotes for ffmpeg concat demuxer
            var safePath = slidePaths[i].Replace("'", "'\\''");
            manifestLines.Add($"file '{safePath}'"); // nói với FFmpeg dùng file ảnh nào
            manifestLines.Add($"duration {Math.Max(durations[i], 0.1).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}"); // Đảm bảo duration tối thiểu 0.1, format số thành 3 chữ số thập phân
        }
        
        var safeLastPath = slidePaths.Last().Replace("'", "'\\''");
        manifestLines.Add($"file '{safeLastPath}'");

        await File.WriteAllLinesAsync(manifestPath, manifestLines, cancellationToken);

        var args = new[]
        {
            "-y",
            "-f", "concat",
            "-safe", "0",
            "-i", manifestPath,
            "-i", audioPath,
            "-vsync", "vfr",
            "-pix_fmt", "yuv420p",
            "-c:v", "libx264",
            "-c:a", "aac",
            "-shortest",
            outputPath
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new Exception("Không thể khởi động FFmpeg process.");

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
        });

        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg error: {Stderr}", stderr);
            throw new Exception($"FFmpeg failed with exit code {process.ExitCode}. See logs.");
        }

        return durations.Sum();
    }
}
