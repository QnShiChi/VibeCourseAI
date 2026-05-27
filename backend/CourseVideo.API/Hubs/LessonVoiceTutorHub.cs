using System.Security.Claims;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CourseVideo.API.Hubs;

[Authorize]
public class LessonVoiceTutorHub : Hub
{
    private readonly ILessonVoiceTutorService _voiceTutorService;
    private readonly ILogger<LessonVoiceTutorHub> _logger;

    public LessonVoiceTutorHub(
        ILessonVoiceTutorService voiceTutorService,
        ILogger<LessonVoiceTutorHub> logger)
    {
        _voiceTutorService = voiceTutorService;
        _logger = logger;
    }

    public async Task CompleteTurn(Guid sessionId, double playbackTimeSeconds, int[] audioBytes)
    {
        try
        {
            var cancellationToken = Context.ConnectionAborted;
            var userId = Guid.Parse(
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User!.FindFirst("sub")!.Value);

            var audioPayload = new byte[audioBytes.Length];
            for (var index = 0; index < audioBytes.Length; index++)
            {
                audioPayload[index] = checked((byte)audioBytes[index]);
            }

            await Clients.Caller.SendAsync("TranscriptionStarted", sessionId, cancellationToken);

            var result = await _voiceTutorService.CompleteTurnAsync(
                sessionId,
                userId,
                playbackTimeSeconds,
                audioPayload,
                cancellationToken);

            await Clients.Caller.SendAsync("TranscriptionCompleted", result.TranscriptionText, cancellationToken);
            await Clients.Caller.SendAsync("AnswerCompleted", result.AnswerText, result.SourceType, cancellationToken);

            foreach (var segment in result.AudioSegments)
            {
                await Clients.Caller.SendAsync(
                    "AnswerAudioSegment",
                    segment.SequenceIndex,
                    segment.Text,
                    segment.AudioUrl,
                    segment.DurationSeconds,
                    cancellationToken);
            }

            await Clients.Caller.SendAsync("AwaitingFollowUpDecision", sessionId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Lesson voice tutor turn failed for session {SessionId}, user {UserId}, payload bytes {ByteCount}.",
                sessionId,
                Context.UserIdentifier ?? "unknown",
                audioBytes?.Length ?? 0);

            throw new HubException(exception.Message);
        }
    }
}
