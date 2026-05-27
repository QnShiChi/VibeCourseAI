namespace CourseVideo.API.Models;

public class Lesson : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string ContentSeed { get; set; } = string.Empty;
    public string? TeachingScript { get; set; }
    public string? SlideOutlineJson { get; set; }
    public string? VoiceoverPlanJson { get; set; }
    public string ContentGenerationStatus { get; set; } = "NotGenerated";
    public DateTime? ContentGeneratedAt { get; set; }
    public string? ContentGenerationError { get; set; }
    public string? VideoUrl { get; set; }
    public string VideoGenerationStatus { get; set; } = "NotGenerated";
    public string? VideoGenerationError { get; set; }
    public DateTime? VideoGeneratedAt { get; set; }
    public string? AudioUrl { get; set; }
    public int? Duration { get; set; }
    public string? AudioSegmentsJson { get; set; }
    public string? NarrationVoiceKey { get; set; }
    public string? TranscriptText { get; set; }
    public string AudioGenerationStatus { get; set; } = "NotGenerated";
    public string? AudioGenerationError { get; set; }
    public DateTime? AudioGeneratedAt { get; set; }
    public bool VoiceTutorEnabled { get; set; } = true;
    public Module? Module { get; set; }
}
