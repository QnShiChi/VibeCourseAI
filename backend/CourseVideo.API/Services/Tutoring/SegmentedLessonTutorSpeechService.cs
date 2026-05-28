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

    public async Task<LessonTutorAudioSegment> SynthesizeSegmentAsync(
        string voiceProfileKey,
        string answerSegment,
        int sequenceIndex,
        CancellationToken cancellationToken)
    {
        var voice = _voiceResolver.Resolve(voiceProfileKey);
        var directory = Path.Combine(_environment.ContentRootPath, "storage", "voice-tutor", "assistant-answers");
        Directory.CreateDirectory(directory);

        var audioBytes = await _edgeTtsService.SynthesizeToBytesAsync(answerSegment, voice, cancellationToken);
        var fileName = $"{Guid.NewGuid():N}.mp3";
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), audioBytes, cancellationToken);
        return new LessonTutorAudioSegment(
            sequenceIndex,
            answerSegment,
            $"/storage/voice-tutor/assistant-answers/{fileName}",
            Math.Max(1, answerSegment.Length / 14d));
    }
}
