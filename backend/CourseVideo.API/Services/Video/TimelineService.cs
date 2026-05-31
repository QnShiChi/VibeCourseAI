using System.Text.Json;
using CourseVideo.API.DTOs.VideoWorker;

namespace CourseVideo.API.Services.Video;

public class TimelineService : ITimelineService
{
    public List<AudioSegment> ParseAudioSegmentsJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<AudioSegment>();
        
        try
        {
            var segments = JsonSerializer.Deserialize<List<AudioSegment>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (segments == null)
            {
                throw new ArgumentException("Audio segments phải là một mảng.");
            }

            foreach (var segment in segments)
            {
                if (segment.DurationSeconds <= 0)
                {
                    throw new ArgumentException("Audio segment phải có durationSeconds > 0.");
                }
            }

            return segments;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Lỗi parse JSON Audio Segments: " + ex.Message);
        }
    }

    public List<SlideItem> ParseSlideOutlineJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<SlideItem>();

        try
        {
            var slides = JsonSerializer.Deserialize<List<SlideItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (slides == null)
            {
                throw new ArgumentException("Slide outline phải là một mảng.");
            }

            return slides;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Lỗi parse JSON Slide Outline: " + ex.Message);
        }
    }

    public List<SlideTimingResponse> BuildSlideTimeline(List<AudioSegment> audioSegments)
    {
        var timeline = new List<SlideTimingResponse>();
        double startSeconds = 0.0;

        foreach (var segment in audioSegments)
        {
            var item = new SlideTimingResponse
            {
                SlideNumber = segment.SlideNumber,
                StartSeconds = startSeconds,
                DurationSeconds = segment.DurationSeconds,
                EndSeconds = startSeconds + segment.DurationSeconds
            };
            timeline.Add(item);
            startSeconds = item.EndSeconds;
        }

        return timeline;
    }
}
