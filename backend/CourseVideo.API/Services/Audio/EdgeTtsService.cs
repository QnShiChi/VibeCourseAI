using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CourseVideo.API.Services.Audio;

public class EdgeTtsService : IEdgeTtsService
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(20);
    private const int MaxDirectTtsChars = 220;
    private const int DefaultChunkChars = 80;
    private readonly ILogger<EdgeTtsService> _logger;
    private readonly string _defaultVoice;

    public EdgeTtsService(IConfiguration configuration, ILogger<EdgeTtsService> logger)
    {
        _logger = logger;
        _defaultVoice = configuration["EDGE_TTS_VOICE"] ?? "vi-VN-HoaiMyNeural";
    }

    public async Task<byte[]> SynthesizeToBytesAsync(string text, string? voice = null, CancellationToken cancellationToken = default)
    {
        var normalizedText = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            throw new ArgumentException("Text to speech không nhận được nội dung narration hợp lệ.");
        }

        if (normalizedText.Length <= MaxDirectTtsChars)
        {
            try
            {
                return await SynthesizeWithRetriesAsync(normalizedText, voice, cancellationToken);
            }
            catch (Exception)
            {
                var fallbackChunks = SplitSentenceIntoFallbackChunks(normalizedText);
                if (fallbackChunks.Count <= 1)
                {
                    throw;
                }

                return await SynthesizeChunkedAsync(fallbackChunks, voice, cancellationToken, continueOnSentenceFailure: false);
            }
        }

        return await SynthesizeParagraphAsync(normalizedText, voice, cancellationToken);
    }

    private async Task<byte[]> SynthesizeParagraphAsync(string text, string? voice, CancellationToken cancellationToken)
    {
        var sentenceChunks = SplitIntoChunksForSynthesis(text);
        return await SynthesizeChunkedAsync(sentenceChunks, voice, cancellationToken, continueOnSentenceFailure: true);
    }

    private async Task<byte[]> SynthesizeChunkedAsync(
        List<string> chunks,
        string? voice,
        CancellationToken cancellationToken,
        bool continueOnSentenceFailure)
    {
        var audioParts = new List<byte[]>();
        foreach (var chunk in chunks.Where(chunk => !string.IsNullOrWhiteSpace(chunk)))
        {
            try
            {
                audioParts.Add(await SynthesizeChunkWithFallbackAsync(chunk, voice, cancellationToken));
            }
            catch (Exception ex) when (continueOnSentenceFailure)
            {
                _logger.LogWarning(ex, "Skipping sentence during audio synthesis after repeated edge-tts failures. Text: {Text}", chunk);
                continue;
            }

            await Task.Delay(300, cancellationToken);
        }

        if (audioParts.Count == 0)
        {
            throw new Exception("edge-tts không synthesize được bất kỳ câu narration nào.");
        }

        return CombineAudio(audioParts);
    }

    private async Task<byte[]> SynthesizeChunkWithFallbackAsync(string text, string? voice, CancellationToken cancellationToken)
    {
        try
        {
            return await SynthesizeWithRetriesAsync(text, voice, cancellationToken);
        }
        catch (Exception)
        {
            var fallbackChunks = SplitSentenceIntoFallbackChunks(text);
            if (fallbackChunks.Count <= 1)
            {
                throw;
            }

            var audioParts = new List<byte[]>();
            foreach (var fallbackChunk in fallbackChunks)
            {
                audioParts.Add(await SynthesizeWithRetriesAsync(fallbackChunk, voice, cancellationToken));
                await Task.Delay(200, cancellationToken);
            }

            return CombineAudio(audioParts);
        }
    }

    private async Task<byte[]> SynthesizeWithRetriesAsync(string text, string? overrideVoice, CancellationToken cancellationToken, int attemptsPerVoice = 4)
    {
        Exception? lastException = null;
        var voices = GetVoiceCandidates(overrideVoice);

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
                        await Task.Delay(500 * (attempt + 1), cancellationToken);
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

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ProcessTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKillProcess(process);
                throw new TimeoutException($"edge-tts vượt quá thời gian chờ {ProcessTimeout.TotalSeconds:0} giây.");
            }

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

    private static void TryKillProcess(Process process)
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
    }

    private List<string> GetVoiceCandidates(string? overrideVoice)
    {
        var fallbackMap = new Dictionary<string, string>
        {
            { "vi-VN-HoaiMyNeural", "vi-VN-NamMinhNeural" },
            { "vi-VN-NamMinhNeural", "vi-VN-HoaiMyNeural" }
        };

        var preferredVoice = string.IsNullOrWhiteSpace(overrideVoice) ? _defaultVoice : overrideVoice.Trim();
        var voices = new List<string> { preferredVoice };
        if (fallbackMap.TryGetValue(preferredVoice, out var fallbackVoice) && !voices.Contains(fallbackVoice))
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

    internal static List<string> SplitIntoChunksForSynthesis(string text, int maxChars = DefaultChunkChars)
    {
        var normalized = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new List<string>();
        }

        return Regex.Split(normalized, @"(?<=[.!?;:])\s+")
            .Select(NormalizeText)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    private static List<string> SplitSentenceIntoFallbackChunks(string text, int maxChars = DefaultChunkChars)
    {
        var normalized = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new List<string>();
        }

        var chunks = new List<string>();
        SplitLongPart(normalized, maxChars, chunks);
        return chunks;
    }

    private static void SplitLongPart(string text, int maxChars, List<string> chunks)
    {
        var normalized = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (normalized.Length <= maxChars)
        {
            chunks.Add(normalized);
            return;
        }

        var phraseParts = Regex.Split(normalized, @"(?<=[,\-])\s+")
            .Select(NormalizeText)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (phraseParts.Count == 0)
        {
            phraseParts.Add(normalized);
        }

        foreach (var phrasePart in phraseParts)
        {
            SplitLongFallbackPart(phrasePart, maxChars, chunks);
        }
    }

    private static void SplitLongFallbackPart(string text, int maxChars, List<string> chunks)
    {
        var normalized = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (normalized.Length <= maxChars)
        {
            chunks.Add(normalized);
            return;
        }

        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
        {
            for (int i = 0; i < normalized.Length; i += maxChars)
            {
                chunks.Add(normalized.Substring(i, Math.Min(maxChars, normalized.Length - i)).Trim());
            }
            return;
        }

        var buffer = new List<string>();
        var currentLength = 0;
        foreach (var word in words)
        {
            var nextLength = buffer.Count == 0 ? word.Length : currentLength + 1 + word.Length;
            if (buffer.Count > 0 && nextLength > maxChars)
            {
                chunks.Add(string.Join(" ", buffer));
                buffer.Clear();
                currentLength = 0;
            }

            buffer.Add(word);
            currentLength = buffer.Count == 1 ? word.Length : currentLength + 1 + word.Length;
        }

        if (buffer.Count > 0)
        {
            chunks.Add(string.Join(" ", buffer));
        }
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
