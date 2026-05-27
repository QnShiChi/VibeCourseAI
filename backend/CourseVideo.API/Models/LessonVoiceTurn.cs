namespace CourseVideo.API.Models;

public class LessonVoiceTurn : BaseEntity
{
    public Guid SessionId { get; set; }
    public int TurnNumber { get; set; }
    public string Status { get; set; } = "Idle";
    public double? PlaybackPausedAtSeconds { get; set; }
    public string? UserAudioUrl { get; set; }
    public string? TranscriptionText { get; set; }
    public decimal? TranscriptionConfidence { get; set; }
    public string? AnswerText { get; set; }
    public string? AnswerSourceSummary { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public LessonVoiceSession? Session { get; set; }
}
