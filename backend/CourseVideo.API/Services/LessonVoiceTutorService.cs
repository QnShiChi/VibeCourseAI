using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonVoiceTutorService : ILessonVoiceTutorService
{
    private readonly ILessonVoiceSessionRepository _sessionRepository;
    private readonly ILessonContextBuilder _contextBuilder;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILessonTutorResponseStreamService _responseStreamService;
    private readonly ILessonTutorSegmenter _segmenter;
    private readonly ILessonTutorSpeechService _speechService;

    public LessonVoiceTutorService(
        ILessonVoiceSessionRepository sessionRepository,
        ILessonContextBuilder contextBuilder,
        ITranscriptionService transcriptionService,
        ILessonTutorResponseStreamService responseStreamService,
        ILessonTutorSegmenter segmenter,
        ILessonTutorSpeechService speechService)
    {
        _sessionRepository = sessionRepository;
        _contextBuilder = contextBuilder;
        _transcriptionService = transcriptionService;
        _responseStreamService = responseStreamService;
        _segmenter = segmenter;
        _speechService = speechService;
    }

    public async Task<LessonVoiceTurnResult> CompleteTurnAsync(
        Guid sessionId,
        Guid userId,
        double playbackTimeSeconds,
        byte[] audioBytes,
        Func<LessonTutorAudioSegment, Task>? onSegmentReady,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.UserId != userId)
        {
            throw new InvalidOperationException("Cannot access another user's voice session.");
        }

        var turnNumber = session.Turns.Count + 1;
        var turn = new LessonVoiceTurn
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TurnNumber = turnNumber,
            Status = "Processing",
            PlaybackPausedAtSeconds = playbackTimeSeconds
        };
        await _sessionRepository.AddTurnAsync(turn, cancellationToken);

        var context = await _contextBuilder.BuildAsync(session.LessonId, playbackTimeSeconds, cancellationToken);
        var transcription = await _transcriptionService.TranscribeAsync(audioBytes, cancellationToken);
        var request = new LessonTutorAnswerRequest(context, transcription.Text, session.ConversationSummary);
        var audioSegments = new List<LessonTutorAudioSegment>();
        var answerParts = new List<string>();
        var sequenceIndex = 0;

        await foreach (var chunk in _responseStreamService.StreamAnswerAsync(request, cancellationToken))
        {
            foreach (var segment in _segmenter.PushText(chunk))
            {
                answerParts.Add(segment);
                var audioSegment = await _speechService.SynthesizeSegmentAsync(
                    session.VoiceProfileKey,
                    segment,
                    sequenceIndex++,
                    cancellationToken);
                audioSegments.Add(audioSegment);
                if (onSegmentReady is not null)
                {
                    await onSegmentReady(audioSegment);
                }
            }
        }

        foreach (var tail in _segmenter.FlushRemaining())
        {
            answerParts.Add(tail);
            var audioSegment = await _speechService.SynthesizeSegmentAsync(
                session.VoiceProfileKey,
                tail,
                sequenceIndex++,
                cancellationToken);
            audioSegments.Add(audioSegment);
            if (onSegmentReady is not null)
            {
                await onSegmentReady(audioSegment);
            }
        }

        var answerText = string.Join(" ", answerParts).Trim();
        turn.Status = "Completed";
        turn.TranscriptionText = transcription.Text;
        turn.TranscriptionConfidence = transcription.Confidence;
        turn.AnswerText = answerText;
        turn.AnswerSourceSummary = "Mixed";
        turn.CompletedAt = DateTime.UtcNow;

        await _sessionRepository.AddMessageAsync(new LessonVoiceMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TurnNumber = turnNumber,
            Role = "User",
            ContentText = transcription.Text,
            ContentSourceType = "Lesson",
            SequenceIndex = 0
        }, cancellationToken);

        await _sessionRepository.AddMessageAsync(new LessonVoiceMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TurnNumber = turnNumber,
            Role = "Assistant",
            ContentText = answerText,
            ContentSourceType = "Mixed",
            AudioUrl = null,
            AudioDurationSeconds = audioSegments.Sum(segment => segment.DurationSeconds),
            SequenceIndex = 1
        }, cancellationToken);

        session.LastPausedVideoTimeSeconds = playbackTimeSeconds;
        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return new LessonVoiceTurnResult(
            "AwaitingFollowUpDecision",
            transcription.Text,
            answerText,
            "Mixed",
            audioSegments);
    }
}
