namespace CourseVideo.API.Models;

public class QuizQuestion : BaseEntity
{
    public Guid QuizId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public Quiz? Quiz { get; set; }
    public ICollection<QuizOption> Options { get; set; } = [];
}
