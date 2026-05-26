using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CourseVideo.API.Services.Audio;

public class EdgeTtsService : IEdgeTtsService
{
    private readonly ILogger<EdgeTtsService> _logger;
    private readonly string _defaultVoice;

    public EdgeTtsService(IConfiguration configuration, ILogger<EdgeTtsService> logger)
    {
        _logger = logger;
        _defaultVoice = configuration["EDGE_TTS_VOICE"] ?? "vi-VN-HoaiMyNeural";
    }

    public async Task<byte[]> SynthesizeToBytesAsync(string text, CancellationToken cancellationToken = default)
    {
        var normalizedText = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            throw new ArgumentException("Text to speech không nhận được nội dung narration hợp lệ.");
        }

        try
        {
            return await SynthesizeWithRetriesAsync(normalizedText, cancellationToken);
        }
        catch (Exception)
        {
            var chunks = SplitIntoChunks(normalizedText);
            if (chunks.Count <= 1)
            {
                throw;
            }

            var audioParts = new List<byte[]>();
            foreach (var chunk in chunks)
            {
                audioParts.Add(await SynthesizeWithRetriesAsync(chunk, cancellationToken));
                await Task.Delay(1500, cancellationToken);
            }

            return CombineAudio(audioParts);
        }
    }

    private async Task<byte[]> SynthesizeWithRetriesAsync(string text, CancellationToken cancellationToken, int attemptsPerVoice = 2)
    {
        Exception? lastException = null;
        var voices = GetVoiceCandidates();

        foreach (var voice in voices)
        {
            for (int attempt = 0; attempt < attemptsPerVoice; attempt++)
            {
                try
                {
                    return await SynthesizeOnceAsync(text, voice, cancellationToken);
                }
                catch (Exception exception)
                {
                    lastException = exception;
                    if (attempt < attemptsPerVoice - 1)
                    {
                        await Task.Delay(750 * (attempt + 1), cancellationToken);
                    }
                }
            }
        }

        throw lastException ?? new Exception("Không thể synthesize audio sau nhiều lần thử.");
    }

    private async Task<byte[]> SynthesizeOnceAsync(string text, string voice, CancellationToken cancellationToken)
    {
        var tempFile = Path.GetTempFileName() + ".mp3";
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "edge-tts",
                Arguments = $"--text \"{text.Replace("\"", "\\\"")}\" --voice {voice} --write-media \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Không thể khởi động tiến trình edge-tts.");
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                throw new Exception($"edge-tts failed with exit code {process.ExitCode}: {error}");
            }

            if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
            {
                throw new Exception("edge-tts không tạo ra file audio hoặc file rỗng.");
            }

            return await File.ReadAllBytesAsync(tempFile, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private List<string> GetVoiceCandidates()
    {
        var fallbackMap = new Dictionary<string, string>
        {
            { "vi-VN-HoaiMyNeural", "vi-VN-NamMinhNeural" },
            { "vi-VN-NamMinhNeural", "vi-VN-HoaiMyNeural" }
        };

        var voices = new List<string> { _defaultVoice };
        if (fallbackMap.TryGetValue(_defaultVoice, out var fallbackVoice) && !voices.Contains(fallbackVoice))
        {
            voices.Add(fallbackVoice);
        }

        return voices;
    }

    private static string NormalizeText(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        normalized = normalized.Replace("•", ", ").Replace("–", "-").Replace("—", "-");
        return normalized;
    }

    private static List<string> SplitIntoChunks(string text, int maxChars = 220)
    {
        var sentenceParts = Regex.Split(text, @"(?<=[.!?;:])\s+")
            .Select(NormalizeText)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (sentenceParts.Count > 1)
        {
            return sentenceParts;
        }

        return SplitLongPart(text, maxChars);
    }

    private static List<string> SplitLongPart(string text, int maxChars)
    {
        var normalized = NormalizeText(text);
        var phraseParts = Regex.Split(normalized, @"(?<=[,])\s+")
            .Select(NormalizeText)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (phraseParts.Count > 1)
        {
            return phraseParts;
        }

        if (normalized.Length > maxChars)
        {
            var chunks = new List<string>();
            for (int i = 0; i < normalized.Length; i += maxChars)
            {
                chunks.Add(normalized.Substring(i, Math.Min(maxChars, normalized.Length - i)).Trim());
            }
            return chunks;
        }

        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
        {
            return new List<string> { normalized };
        }

        int midpoint = Math.Max(1, words.Length / 2);
        var firstHalf = string.Join(" ", words.Take(midpoint)).Trim();
        var secondHalf = string.Join(" ", words.Skip(midpoint)).Trim();

        return new List<string> { firstHalf, secondHalf }.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }

    private static byte[] CombineAudio(List<byte[]> parts)
    {
        int totalLength = parts.Sum(p => p.Length);
        byte[] combined = new byte[totalLength];
        int offset = 0;
        foreach (var part in parts)
        {
            Buffer.BlockCopy(part, 0, combined, offset, part.Length);
            offset += part.Length;
        }
        return combined;
    }
}
