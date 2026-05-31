namespace CourseVideo.API.DTOs.OpenRouter;

public class OpenRouterQuizGenerationResult
{
    public string Title { get; set; } = string.Empty;
    public List<OpenRouterQuizQuestionResult> Questions { get; set; } = [];
}

public class OpenRouterQuizQuestionResult
{
    public string QuestionText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<OpenRouterQuizOptionResult> Options { get; set; } = [];
}

public class OpenRouterQuizOptionResult
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
