namespace CourseVideo.API.Models;

public class Quiz : BaseEntity
{
    public Guid? LessonId { get; set; }
    public Guid? CourseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SourceContentVersion { get; set; }
    public int QuestionCount { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public string? GenerationError { get; set; }
    public Lesson? Lesson { get; set; }
    public Course? Course { get; set; }
    public ICollection<QuizQuestion> Questions { get; set; } = [];
    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}
