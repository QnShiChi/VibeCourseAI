namespace CourseVideo.API.DTOs.LessonVoiceTutor;

public class LessonVoiceMessageResponse
{
    public Guid MessageId { get; set; }
    public int TurnNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
    public string ContentSourceType { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public double? AudioDurationSeconds { get; set; }
    public int SequenceIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}
