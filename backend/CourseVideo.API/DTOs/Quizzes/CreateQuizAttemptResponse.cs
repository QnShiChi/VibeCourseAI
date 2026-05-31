namespace CourseVideo.API.DTOs.Quizzes;

public class CreateQuizAttemptResponse
{
    public Guid AttemptId { get; set; }
    public DateTime StartedAt { get; set; }
}
