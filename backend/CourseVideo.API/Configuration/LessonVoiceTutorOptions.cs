namespace CourseVideo.API.Configuration;

public class LessonVoiceTutorOptions
{
    public int QuestionAudioMaxSeconds { get; set; } = 30;
    public int FollowUpLimit { get; set; } = 8;
    public int SessionTtlMinutes { get; set; } = 15;
}
