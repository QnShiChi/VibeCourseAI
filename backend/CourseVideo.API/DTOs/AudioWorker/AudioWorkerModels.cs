using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.AudioWorker;

public class AudioWorkerLessonRequest
{
    [JsonPropertyName("lesson_id")]
    public Guid LessonId { get; set; }

    [JsonPropertyName("teaching_script")]
    public string TeachingScript { get; set; } = string.Empty;

    [JsonPropertyName("slide_outline_json")]
    public string SlideOutlineJson { get; set; } = string.Empty;

    [JsonPropertyName("voiceover_plan_json")]
    public string VoiceoverPlanJson { get; set; } = string.Empty;
}

public class AudioWorkerLessonResponse
{
    [JsonPropertyName("audio_url")]
    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("duration_seconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("segments")]
    public List<AudioWorkerSegmentResponse> Segments { get; set; } = new();

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public class AudioWorkerSegmentResponse
{
    [JsonPropertyName("slide_number")]
    public int SlideNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("narration_text")]
    public string NarrationText { get; set; } = string.Empty;

    [JsonPropertyName("audio_url")]
    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("duration_seconds")]
    public double DurationSeconds { get; set; }
}

public class NarrationSegment
{
    public int SlideNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
}
