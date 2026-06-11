namespace CourseVideo.API.Models;

public class LessonVoiceSession : BaseEntity
{
    public Guid LessonId { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public double? LastPausedVideoTimeSeconds { get; set; }
    public string VoiceProfileKey { get; set; } = string.Empty;
    public string ContextScope { get; set; } = "LessonWithCourseAndExternalKnowledge";
    public string? ConversationSummary { get; set; }
    public Lesson? Lesson { get; set; }
    public User? User { get; set; }
    public List<LessonVoiceTurn> Turns { get; set; } = [];
    public List<LessonVoiceMessage> Messages { get; set; } = [];
}
