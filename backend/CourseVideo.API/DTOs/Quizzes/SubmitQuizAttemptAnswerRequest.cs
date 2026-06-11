namespace CourseVideo.API.DTOs.Quizzes;

public class SubmitQuizAttemptAnswerRequest
{
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
}
