using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.VideoWorker;

public class VideoWorkerLessonRequest
{
    [JsonPropertyName("lesson_id")]
    public string LessonId { get; set; } = string.Empty;

    [JsonPropertyName("lesson_title")]
    public string LessonTitle { get; set; } = string.Empty;

    [JsonPropertyName("slide_outline_json")]
    public string SlideOutlineJson { get; set; } = string.Empty;

    [JsonPropertyName("audio_url")]
    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("audio_segments_json")]
    public string AudioSegmentsJson { get; set; } = string.Empty;
}

public class SlideTimingResponse
{
    [JsonPropertyName("slide_number")]
    public int SlideNumber { get; set; }

    [JsonPropertyName("start_seconds")]
    public double StartSeconds { get; set; }

    [JsonPropertyName("duration_seconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("end_seconds")]
    public double EndSeconds { get; set; }
}

public class VideoWorkerLessonResponse
{
    [JsonPropertyName("video_url")]
    public string VideoUrl { get; set; } = string.Empty;

    [JsonPropertyName("duration_seconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("slide_timings")]
    public List<SlideTimingResponse> SlideTimings { get; set; } = new();
}

public class SlideItem
{
    [JsonPropertyName("slideNumber")]
    public int SlideNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("imageKeyword")]
    public string ImageKeyword { get; set; } = string.Empty;

    [JsonPropertyName("bulletPoints")]
    public List<string> BulletPoints { get; set; } = new();

    [JsonPropertyName("speakerNotes")]
    public string SpeakerNotes { get; set; } = string.Empty;
}

public class AudioSegment
{
    [JsonPropertyName("slideNumber")]
    public int SlideNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }
}
