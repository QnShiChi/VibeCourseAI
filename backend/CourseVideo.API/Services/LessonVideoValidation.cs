using CourseVideo.API.Models;

namespace CourseVideo.API.Services;

public static class LessonVideoValidation
{
    public static void ValidateReadyForVideo(Lesson lesson)
    {
        if (string.IsNullOrWhiteSpace(lesson.SlideOutlineJson))
        {
            throw new InvalidOperationException("Lesson phải có slide outline hợp lệ trước khi render video.");
        }

        if (!string.Equals(lesson.AudioGenerationStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(lesson.AudioUrl))
        {
            throw new InvalidOperationException("Lesson chưa có audio để render video.");
        }

        if (string.IsNullOrWhiteSpace(lesson.AudioSegmentsJson))
        {
            throw new InvalidOperationException("Lesson chưa có metadata segment audio để render video.");
        }

        SlideOutlineValidation.ParseAndValidate(lesson.SlideOutlineJson);
    }
}
