namespace CourseVideo.API.Models;

public class LessonVoiceMessage : BaseEntity
{
    public Guid SessionId { get; set; }
    public int TurnNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
    public string ContentSourceType { get; set; } = "Lesson";
    public string? AudioUrl { get; set; }
    public double? AudioDurationSeconds { get; set; }
    public int SequenceIndex { get; set; }
    public LessonVoiceSession? Session { get; set; }
}
