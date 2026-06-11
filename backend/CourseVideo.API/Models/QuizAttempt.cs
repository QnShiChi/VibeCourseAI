namespace CourseVideo.API.Models;

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal Score { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public Quiz? Quiz { get; set; }
    public User? User { get; set; }
    public ICollection<QuizAttemptAnswer> Answers { get; set; } = [];
}
