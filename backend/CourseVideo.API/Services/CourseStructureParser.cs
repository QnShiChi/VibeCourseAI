using System.Text.RegularExpressions;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public partial class CourseStructureParser : ICourseStructureParser
{
    public ParsedCourseStructure Parse(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return BuildFallbackStructure(string.Empty);
        }

        var normalizedLines = extractedText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var parsed = ParseByHeadings(normalizedLines);
        return parsed.Modules.Count > 0 ? parsed : BuildFallbackStructure(extractedText);
    }

    private static ParsedCourseStructure ParseByHeadings(IReadOnlyList<string> lines)
    {
        var result = new ParsedCourseStructure();
        ParsedModuleStructure? currentModule = null;
        ParsedLessonStructure? currentLesson = null;
        var currentLessonContent = new List<string>();

        void FlushCurrentLesson()
        {
            if (currentLesson is null)
            {
                return;
            }

            currentLesson.ContentSeed = JoinContent(currentLessonContent, currentLesson.Description);
            if (string.IsNullOrWhiteSpace(currentLesson.Description))
            {
                currentLesson.Description = SummarizeContent(currentLesson.ContentSeed);
            }

            currentLessonContent.Clear();
            currentLesson = null;
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (IsModuleHeading(line))
            {
                FlushCurrentLesson();

                currentModule = new ParsedModuleStructure
                {
                    Title = line,
                    Description = string.Empty
                };
                result.Modules.Add(currentModule);
                continue;
            }

            if (IsLessonHeading(line))
            {
                if (currentModule is null)
                {
                    currentModule = new ParsedModuleStructure
                    {
                        Title = "Tong quan khoa hoc",
                        Description = "Module khoi tao tu de cuong khong co heading module ro rang."
                    };
                    result.Modules.Add(currentModule);
                }

                FlushCurrentLesson();

                currentLesson = new ParsedLessonStructure
                {
                    Title = line,
                    Description = string.Empty
                };
                currentModule.Lessons.Add(currentLesson);
                continue;
            }

            if (currentLesson is null)
            {
                if (currentModule is null)
                {
                    continue;
                }

                currentModule.Description = AppendSentence(currentModule.Description, line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(currentLesson.Description))
            {
                currentLesson.Description = line;
            }

            currentLessonContent.Add(line);
        }

        FlushCurrentLesson();

        result.Modules = result.Modules
            .Where(module => module.Lessons.Count > 0)
            .ToList();

        return result;
    }

    private static ParsedCourseStructure BuildFallbackStructure(string extractedText)
    {
        var blocks = Regex.Split(extractedText.Trim(), @"\n\s*\n")
            .Select(block => block.Trim())
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToList();

        if (blocks.Count == 0)
        {
            blocks = [extractedText.Trim()];
        }

        var lessons = blocks
            .Select((block, index) => new ParsedLessonStructure
            {
                Title = $"Bai {index + 1}",
                Description = SummarizeContent(block),
                ContentSeed = block
            })
            .Where(lesson => !string.IsNullOrWhiteSpace(lesson.ContentSeed))
            .ToList();

        return new ParsedCourseStructure
        {
            Modules =
            [
                new ParsedModuleStructure
                {
                    Title = "Tong quan khoa hoc",
                    Description = "Cau truc fallback duoc tao tu noi dung de cuong hien co.",
                    Lessons = lessons
                }
            ]
        };
    }

    private static bool IsModuleHeading(string line)
    {
        return ModuleHeadingRegex().IsMatch(line);
    }

    private static bool IsLessonHeading(string line)
    {
        return LessonHeadingRegex().IsMatch(line);
    }

    private static string AppendSentence(string current, string next)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return next;
        }

        return $"{current} {next}";
    }

    private static string JoinContent(IReadOnlyCollection<string> lines, string fallback)
    {
        var content = string.Join(Environment.NewLine, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        return string.IsNullOrWhiteSpace(content) ? fallback : content;
    }

    private static string SummarizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Noi dung bai hoc duoc trich tu de cuong.";
        }

        var normalized = Regex.Replace(content, @"\s+", " ").Trim();
        return normalized.Length <= 160 ? normalized : $"{normalized[..160].Trim()}...";
    }

    [GeneratedRegex(@"^(chuong|chương|phan|phần|module|unit)\s+\d+[\.: -]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ModuleHeadingRegex();

    [GeneratedRegex(@"^(bai|bài|lesson)\s+\d+[\.: -]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LessonHeadingRegex();
}
