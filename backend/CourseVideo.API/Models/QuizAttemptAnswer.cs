namespace CourseVideo.API.Models;

public class QuizAttemptAnswer : BaseEntity
{
    public Guid QuizAttemptId { get; set; }
    public Guid QuizQuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public QuizAttempt? QuizAttempt { get; set; }
    public QuizQuestion? QuizQuestion { get; set; }
}
