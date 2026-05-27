using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonVoiceTutorService : ILessonVoiceTutorService
{
    private readonly ILessonVoiceSessionRepository _sessionRepository;
    private readonly ILessonContextBuilder _contextBuilder;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILessonTutorAnswerService _answerService;
    private readonly ILessonTutorSpeechService _speechService;

    public LessonVoiceTutorService(
        ILessonVoiceSessionRepository sessionRepository,
        ILessonContextBuilder contextBuilder,
        ITranscriptionService transcriptionService,
        ILessonTutorAnswerService answerService,
        ILessonTutorSpeechService speechService)
    {
        _sessionRepository = sessionRepository;
        _contextBuilder = contextBuilder;
        _transcriptionService = transcriptionService;
        _answerService = answerService;
        _speechService = speechService;
    }

    public async Task<LessonVoiceTurnResult> CompleteTurnAsync(
        Guid sessionId,
        Guid userId,
        double playbackTimeSeconds,
        byte[] audioBytes,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.UserId != userId)
        {
            throw new InvalidOperationException("Cannot access another user's voice session.");
        }

        var turnNumber = session.Turns.Count + 1;
        await _sessionRepository.AddTurnAsync(new LessonVoiceTurn
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TurnNumber = turnNumber,
            Status = "Processing",
            PlaybackPausedAtSeconds = playbackTimeSeconds
        }, cancellationToken);

        var context = await _contextBuilder.BuildAsync(session.LessonId, playbackTimeSeconds, cancellationToken);
        var transcription = await _transcriptionService.TranscribeAsync(audioBytes, cancellationToken);
        var answer = await _answerService.GenerateAnswerAsync(
            new LessonTutorAnswerRequest(context, transcription.Text, session.ConversationSummary),
            cancellationToken);
        var audioSegments = await _speechService.SynthesizeAsync(session.VoiceProfileKey, answer.AnswerText, cancellationToken);

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
            ContentText = answer.AnswerText,
            ContentSourceType = answer.SourceType,
            AudioUrl = audioSegments.FirstOrDefault()?.AudioUrl,
            AudioDurationSeconds = audioSegments.Sum(segment => segment.DurationSeconds),
            SequenceIndex = 1
        }, cancellationToken);

        session.LastPausedVideoTimeSeconds = playbackTimeSeconds;
        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return new LessonVoiceTurnResult(
            "AwaitingFollowUpDecision",
            transcription.Text,
            answer.AnswerText,
            answer.SourceType,
            audioSegments);
    }
}
