namespace CourseVideo.API.DTOs.LessonVoiceTutor;

public class LessonVoiceSessionResponse
{
    public Guid SessionId { get; set; }
    public Guid LessonId { get; set; }
    public Guid CourseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string VoiceProfileKey { get; set; } = string.Empty;
    public double? LastPausedVideoTimeSeconds { get; set; }
}
