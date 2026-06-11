using System.Text;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services.Tutoring;

public class LessonTutorSegmenter : ILessonTutorSegmenter
{
    private const int SoftCharacterLimit = 160;
    private readonly StringBuilder _buffer = new();

    public IReadOnlyList<string> PushText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        _buffer.Append(text);
        return DrainCompletedSegments();
    }

    public IReadOnlyList<string> FlushRemaining()
    {
        var remaining = Normalize(_buffer.ToString());
        _buffer.Clear();
        return string.IsNullOrWhiteSpace(remaining) ? [] : [EnsureTerminalPunctuation(remaining)];
    }

    private List<string> DrainCompletedSegments()
    {
        var output = new List<string>();

        while (TryExtractSegment(out var segment))
        {
            output.Add(segment);
        }

        return output;
    }

    private bool TryExtractSegment(out string segment)
    {
        segment = string.Empty;
        var current = _buffer.ToString();
        if (string.IsNullOrWhiteSpace(current))
        {
            return false;
        }

        var punctuationIndex = current.LastIndexOfAny(['.', '!', '?', '\n']);
        if (punctuationIndex >= 0 && punctuationIndex + 1 <= current.Length)
        {
            segment = Normalize(current[..(punctuationIndex + 1)]);
            _buffer.Remove(0, punctuationIndex + 1);
            return !string.IsNullOrWhiteSpace(segment);
        }

        if (current.Length < SoftCharacterLimit)
        {
            return false;
        }

        var commaIndex = current.LastIndexOf(',');
        var splitIndex = commaIndex >= 0 ? commaIndex + 1 : SoftCharacterLimit;
        segment = EnsureTerminalPunctuation(Normalize(current[..splitIndex]));
        _buffer.Remove(0, splitIndex);
        return !string.IsNullOrWhiteSpace(segment);
    }

    private static string Normalize(string value)
    {
        return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string EnsureTerminalPunctuation(string value)
    {
        return value.EndsWith('.') || value.EndsWith('!') || value.EndsWith('?')
            ? value
            : $"{value}.";
    }
}
