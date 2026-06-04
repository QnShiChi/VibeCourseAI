namespace CourseVideo.API.DTOs.Quizzes;

public class SubmitQuizAttemptResponse
{
    public Guid AttemptId { get; set; }
    public decimal Score { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public IReadOnlyList<SubmitQuizAttemptAnswerResultResponse> Answers { get; set; } = [];
}

public class SubmitQuizAttemptAnswerResultResponse
{
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public Guid CorrectOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
