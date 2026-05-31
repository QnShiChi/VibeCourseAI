namespace CourseVideo.API.DTOs.Quizzes;

public class SubmitQuizAttemptRequest
{
    public List<SubmitQuizAttemptAnswerRequest> Answers { get; set; } = [];
}
