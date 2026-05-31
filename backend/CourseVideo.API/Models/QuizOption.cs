namespace CourseVideo.API.Models;

public class QuizOption : BaseEntity
{
    public Guid QuizQuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsCorrect { get; set; }
    public QuizQuestion? QuizQuestion { get; set; }
}
