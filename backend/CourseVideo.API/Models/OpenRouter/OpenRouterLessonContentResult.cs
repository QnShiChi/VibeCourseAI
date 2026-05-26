namespace CourseVideo.API.Models.OpenRouter;

public class OpenRouterLessonContentResult
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string TeachingScript { get; set; } = string.Empty;
    public IReadOnlyList<OpenRouterSlideOutlineResult> SlideOutline { get; set; } = [];
    public OpenRouterVoiceoverPlanResult VoiceoverPlan { get; set; } = new();
}

public class OpenRouterSlideOutlineResult
{
    public int SlideNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageKeyword { get; set; } = string.Empty;
    public IReadOnlyList<string> BulletPoints { get; set; } = [];
    public string SpeakerNotes { get; set; } = string.Empty;
}

public class OpenRouterVoiceoverPlanResult
{
    public int EstimatedDurationMinutes { get; set; }
    public string Tone { get; set; } = string.Empty;
    public string Pacing { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string PronunciationNotes { get; set; } = string.Empty;
}
