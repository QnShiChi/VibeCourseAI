namespace CourseVideo.API.DTOs.Quizzes;

public class QuizQuestionResponse
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public IReadOnlyList<QuizOptionResponse> Options { get; set; } = [];
}
