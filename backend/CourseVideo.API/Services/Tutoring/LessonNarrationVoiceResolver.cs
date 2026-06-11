namespace CourseVideo.API.Services.Tutoring;

public class LessonNarrationVoiceResolver
{
    public string Resolve(string? voiceProfileKey)
    {
        return string.IsNullOrWhiteSpace(voiceProfileKey) ? "vi-VN-HoaiMyNeural" : voiceProfileKey.Trim();
    }
}
