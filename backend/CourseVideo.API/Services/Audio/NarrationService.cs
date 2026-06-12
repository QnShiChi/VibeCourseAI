using System.Text.Json;
using System.Text.RegularExpressions;
using CourseVideo.API.DTOs.AudioWorker;

namespace CourseVideo.API.Services.Audio;

public class NarrationService : INarrationService
{
    private static readonly (Regex Pattern, string Replacement)[] ForbiddenNarrationPatterns =
    [
        (new Regex(@"\bở slide này\b[:,]?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), string.Empty),
        (new Regex(@"\btrong slide này\b[:,]?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), string.Empty),
        (new Regex(@"\bslide này\b[:,]?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), string.Empty),
        (new Regex(@"\bslide tiếp theo\b[:,]?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "Tiếp theo, "),
        (new Regex(@"\bslide cuối cùng\b[:,]?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "Cuối cùng, "),
        (new Regex(@"\btrên màn hình\b[:,]?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), string.Empty),
        (new Regex(@"\bnhìn vào\b[:,]?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "xem xét ")
    ];

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

        if (string.IsNullOrWhiteSpace(teachingScript))
        {
            throw new ArgumentException("Teaching script không được để trống.");
        }

        var scriptSegments = BuildScriptSegments(teachingScript, slideOutline.Count);
        var segments = new List<NarrationSegment>();
        int index = 1;

        foreach (var slide in slideOutline)
        {
            var title = GetStringValue(slide, "title", "Title");
            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"Phan {index}";
            }

            var narrationText = NormalizeNarrationText(scriptSegments[index - 1]);

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

    private static List<string> BuildScriptSegments(string teachingScript, int segmentCount)
    {
        var paragraphUnits = SplitParagraphs(teachingScript);
        if (paragraphUnits.Count >= segmentCount)
        {
            return DistributeUnits(paragraphUnits, segmentCount);
        }

        var sentenceUnits = SplitSentences(teachingScript);
        if (sentenceUnits.Count >= segmentCount)
        {
            return DistributeUnits(sentenceUnits, segmentCount);
        }

        return SplitByWordCount(teachingScript, segmentCount);
    }

    private static List<string> SplitParagraphs(string teachingScript)
    {
        return Regex
            .Split(teachingScript.Replace("\r\n", "\n"), @"\n\s*\n")
            .Select(CollapseWhitespace)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();
    }

    private static List<string> SplitSentences(string teachingScript)
    {
        return Regex
            .Split(CollapseWhitespace(teachingScript), @"(?<=[\.\!\?\;\:])\s+")
            .Select(CollapseWhitespace)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();
    }

    private static List<string> DistributeUnits(IReadOnlyList<string> units, int segmentCount)
    {
        var segments = new List<string>(segmentCount);
        var currentIndex = 0;

        for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            var remainingUnits = units.Count - currentIndex;
            var remainingSegments = segmentCount - segmentIndex;
            var takeCount = (int)Math.Ceiling((double)remainingUnits / remainingSegments);
            var segmentText = string.Join(" ", units.Skip(currentIndex).Take(takeCount)).Trim();
            segments.Add(segmentText);
            currentIndex += takeCount;
        }

        return segments;
    }

    private static List<string> SplitByWordCount(string teachingScript, int segmentCount)
    {
        var words = CollapseWhitespace(teachingScript)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length < segmentCount)
        {
            throw new ArgumentException("Teaching script quá ngắn để chia theo số lượng slide.");
        }

        var segments = new List<string>(segmentCount);
        var currentIndex = 0;

        for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            var remainingWords = words.Length - currentIndex;
            var remainingSegments = segmentCount - segmentIndex;
            var takeCount = (int)Math.Ceiling((double)remainingWords / remainingSegments);
            segments.Add(string.Join(" ", words.Skip(currentIndex).Take(takeCount)));
            currentIndex += takeCount;
        }

        return segments;
    }

    private static string CollapseWhitespace(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private static string NormalizeNarrationText(string narrationText)
    {
        var normalized = narrationText;

        foreach (var (pattern, replacement) in ForbiddenNarrationPatterns)
        {
            normalized = pattern.Replace(normalized, replacement);
        }

        normalized = CollapseWhitespace(normalized);
        normalized = Regex.Replace(normalized, @"^[,;:\-\s]+", string.Empty);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(normalized[0]) + normalized[1..];
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
}
