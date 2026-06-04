using System.Text.Json;
using CourseVideo.API.DTOs.AudioWorker;

namespace CourseVideo.API.Services.Audio;

public class NarrationService : INarrationService
{
    public List<NarrationSegment> BuildNarrationSegments(string teachingScript, string slideOutlineJson, string voiceoverPlanJson)
    {
        if (string.IsNullOrWhiteSpace(slideOutlineJson))
        {
            throw new ArgumentException("Slide outline không được để trống.");
        }

        var slideOutline = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(slideOutlineJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (slideOutline == null || slideOutline.Count == 0)
        {
            throw new ArgumentException("Slide outline phải là một mảng slide không rỗng.");
        }

        var segments = new List<NarrationSegment>();
        int index = 1;

        foreach (var slide in slideOutline)
        {
            var notes = GetStringValue(slide, "speakerNotes", "SpeakerNotes");
            var bullets = GetListValue(slide, "bulletPoints", "BulletPoints");
            var title = GetStringValue(slide, "title", "Title");
            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"Slide {index}";
            }

            var bulletText = string.Join("; ", bullets.Where(b => !string.IsNullOrWhiteSpace(b)));
            var narrationText = notes;

            if (narrationText.Length < 30)
            {
                var parts = new List<string> { $"Ở slide này: {title}." };
                if (!string.IsNullOrWhiteSpace(bulletText))
                {
                    parts.Add(bulletText);
                }
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    parts.Add(notes);
                }
                narrationText = string.Join(" ", parts).Trim();
            }

            if (string.IsNullOrWhiteSpace(narrationText))
            {
                throw new ArgumentException($"Missing narration text for slide {index}.");
            }

            var slideNumberRaw = GetStringValue(slide, "slideNumber", "SlideNumber");
            int slideNumber = index;
            if (int.TryParse(slideNumberRaw, out int parsed))
            {
                slideNumber = parsed;
            }

            segments.Add(new NarrationSegment
            {
                SlideNumber = slideNumber,
                Title = title,
                NarrationText = narrationText
            });

            index++;
        }

        return segments;
    }

    private string GetStringValue(Dictionary<string, object> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString() ?? string.Empty;
                }
                else if (element.ValueKind == JsonValueKind.Number)
                {
                    return element.GetRawText();
                }
            }
            else if (dict.TryGetValue(key, out var strVal) && strVal != null)
            {
                return strVal.ToString() ?? string.Empty;
            }
        }
        return string.Empty;
    }

    private List<string> GetListValue(Dictionary<string, object> dict, params string[] keys)
    {
        var list = new List<string>();
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out var val) && val is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            list.Add(item.GetString() ?? string.Empty);
                        }
                    }
                    return list;
                }
            }
        }
        return list;
    }
}
