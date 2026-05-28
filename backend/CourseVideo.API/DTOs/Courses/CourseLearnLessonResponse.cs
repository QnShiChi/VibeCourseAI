namespace CourseVideo.API.DTOs.Courses;

public class CourseLearnLessonResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string ContentSeed { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string VideoGenerationStatus { get; set; } = string.Empty;
    public string VideoGenerationError { get; set; } = string.Empty;
    public int? Duration { get; set; }
    public Guid? QuizId { get; set; }
    public string QuizStatus { get; set; } = string.Empty;
    public int QuizQuestionCount { get; set; }
}
