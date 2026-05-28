namespace CourseVideo.API.DTOs.Quizzes;

public class QuizAttemptHistoryItemResponse
{
    public Guid AttemptId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal Score { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
}
