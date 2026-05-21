using CourseVideo.API.Models;

namespace CourseVideo.API.Services;

public static class LessonAudioValidation
{
    public static void ValidateReadyForAudio(Lesson lesson)
    {
        if (string.IsNullOrWhiteSpace(lesson.TeachingScript))
        {
            throw new InvalidOperationException("Lesson phải có teaching script trước khi generate audio.");
        }

        if (string.IsNullOrWhiteSpace(lesson.SlideOutlineJson))
        {
            throw new InvalidOperationException("Lesson phải có slide outline hợp lệ trước khi generate audio.");
        }

        if (string.IsNullOrWhiteSpace(lesson.VoiceoverPlanJson))
        {
            throw new InvalidOperationException("Lesson phải có voiceover plan hợp lệ trước khi generate audio.");
        }

        SlideOutlineValidation.ParseAndValidate(lesson.SlideOutlineJson);
        VoiceoverPlanValidation.ParseAndValidate(lesson.VoiceoverPlanJson);
    }
}
