namespace CourseVideo.API.DTOs.Quizzes;

public class QuizResponse
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public IReadOnlyList<QuizQuestionResponse> Questions { get; set; } = [];
}
