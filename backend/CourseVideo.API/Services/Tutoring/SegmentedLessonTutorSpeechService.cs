using CourseVideo.API.Services.Audio;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services.Tutoring;

public class SegmentedLessonTutorSpeechService : ILessonTutorSpeechService
{
    private readonly IEdgeTtsService _edgeTtsService;
    private readonly LessonNarrationVoiceResolver _voiceResolver;
    private readonly IWebHostEnvironment _environment;

    public SegmentedLessonTutorSpeechService(
        IEdgeTtsService edgeTtsService,
        LessonNarrationVoiceResolver voiceResolver,
        IWebHostEnvironment environment)
    {
        _edgeTtsService = edgeTtsService;
        _voiceResolver = voiceResolver;
        _environment = environment;
    }

    public async Task<IReadOnlyList<LessonTutorAudioSegment>> SynthesizeAsync(string voiceProfileKey, string answerText, CancellationToken cancellationToken)
    {
        var voice = _voiceResolver.Resolve(voiceProfileKey);
        var segments = SplitAnswer(answerText);
        var output = new List<LessonTutorAudioSegment>();
        var directory = Path.Combine(_environment.ContentRootPath, "storage", "voice-tutor", "assistant-answers");
        Directory.CreateDirectory(directory);

        for (var index = 0; index < segments.Count; index++)
        {
            var audioBytes = await _edgeTtsService.SynthesizeToBytesAsync(segments[index], voice, cancellationToken);
            var fileName = $"{Guid.NewGuid():N}.mp3";
            await File.WriteAllBytesAsync(Path.Combine(directory, fileName), audioBytes, cancellationToken);
            output.Add(new LessonTutorAudioSegment(
                index,
                segments[index],
                $"/storage/voice-tutor/assistant-answers/{fileName}",
                Math.Max(1, segments[index].Length / 14d)));
        }

        return output;
    }

    private static List<string> SplitAnswer(string answerText)
    {
        var segments = answerText
            .Split(['\n', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Trim())
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(segment => segment.EndsWith('.') ? segment : $"{segment}.")
            .ToList();

        return segments.Count > 0 ? segments : [answerText.Trim()];
    }
}
