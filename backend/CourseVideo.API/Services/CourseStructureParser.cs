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
            .Select(DensifyModuleLessons)
            .ToList();

        return result;
    }

    private static ParsedCourseStructure BuildFallbackStructure(string extractedText)
    {
        var lessonBlocks = ExtractLessonBlocks(extractedText);
        var lessons = lessonBlocks
            .Select(CreateFallbackLesson)
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

    private static ParsedModuleStructure DensifyModuleLessons(ParsedModuleStructure module)
    {
        var denseLessons = new List<ParsedLessonStructure>();
        foreach (var lesson in module.Lessons)
        {
            var expandedLessons = ExpandLessonIfNeeded(lesson, module.Lessons.Count);
            denseLessons.AddRange(expandedLessons);
        }

        module.Lessons = denseLessons
            .Select((lesson, index) =>
            {
                lesson.Title = string.IsNullOrWhiteSpace(lesson.Title) ? $"Bai {index + 1}" : lesson.Title.Trim();
                lesson.Description = string.IsNullOrWhiteSpace(lesson.Description)
                    ? SummarizeContent(lesson.ContentSeed)
                    : lesson.Description.Trim();
                lesson.ContentSeed = lesson.ContentSeed.Trim();
                return lesson;
            })
            .Where(lesson => !string.IsNullOrWhiteSpace(lesson.ContentSeed))
            .ToList();

        return module;
    }

    private static List<ParsedLessonStructure> ExpandLessonIfNeeded(ParsedLessonStructure lesson, int lessonCountInModule)
    {
        if (string.IsNullOrWhiteSpace(lesson.ContentSeed))
        {
            return [lesson];
        }

        var segments = SegmentLinesBySubtopic(lesson.ContentSeed);
        if (segments.Count < 2)
        {
            return [lesson];
        }

        if (lessonCountInModule >= 4 && segments.Count <= 2)
        {
            return [lesson];
        }

        return segments
            .Select((segment, index) => new ParsedLessonStructure
            {
                Title = BuildExpandedLessonTitle(lesson.Title, segment, index + 1),
                Description = SummarizeContent(segment),
                ContentSeed = segment
            })
            .ToList();
    }

    private static List<string> ExtractLessonBlocks(string extractedText)
    {
        var normalized = extractedText.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var bulletBlocks = ExtractBulletBlocks(normalized);
        if (bulletBlocks.Count >= 2)
        {
            return bulletBlocks;
        }

        var bulletSegments = SegmentLinesBySubtopic(normalized);
        if (bulletSegments.Count >= 2)
        {
            return bulletSegments;
        }

        var blocks = Regex.Split(normalized, @"\n\s*\n")
            .Select(block => block.Trim())
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToList();

        if (blocks.Count >= 2)
        {
            return blocks;
        }

        var lines = normalized
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count >= 6)
        {
            return lines
                .Chunk(2)
                .Select(chunk => string.Join(Environment.NewLine, chunk))
                .ToList();
        }

        return [normalized];
    }

    private static List<string> ExtractBulletBlocks(string content)
    {
        var lines = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var bulletIndices = lines
            .Select((line, index) => new { line, index })
            .Where(item => IsSubtopicLine(item.line))
            .Select(item => item.index)
            .ToList();

        if (bulletIndices.Count < 3)
        {
            return [];
        }

        var blocks = new List<string>();
        for (var index = 0; index < bulletIndices.Count; index++)
        {
            var start = bulletIndices[index];
            var end = index + 1 < bulletIndices.Count ? bulletIndices[index + 1] : lines.Count;
            var chunk = lines.Skip(start).Take(end - start).ToList();
            if (index == 0 && start > 0)
            {
                chunk.Insert(0, lines[start - 1]);
            }

            blocks.Add(string.Join(Environment.NewLine, chunk));
        }

        return blocks;
    }

    private static ParsedLessonStructure CreateFallbackLesson(string block, int index)
    {
        var titleLine = block
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        var resolvedTitle = NormalizeBulletTitle(titleLine);

        return new ParsedLessonStructure
        {
            Title = string.IsNullOrWhiteSpace(resolvedTitle) ? $"Bai {index + 1}" : resolvedTitle,
            Description = SummarizeContent(block),
            ContentSeed = block
        };
    }

    private static List<string> SegmentLinesBySubtopic(string content)
    {
        var lines = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count < 2)
        {
            return [];
        }

        var segments = new List<string>();
        var currentSegment = new List<string>();

        foreach (var line in lines)
        {
            if (currentSegment.Count > 0 && IsSubtopicLine(line))
            {
                segments.Add(string.Join(Environment.NewLine, currentSegment));
                currentSegment = [];
            }

            currentSegment.Add(line);
        }

        if (currentSegment.Count > 0)
        {
            segments.Add(string.Join(Environment.NewLine, currentSegment));
        }

        return segments.Count >= 2 && segments.All(segment => segment.Length >= 12)
            ? segments
            : [];
    }

    private static bool IsSubtopicLine(string line)
    {
        return BulletOrNumberedLineRegex().IsMatch(line);
    }

    private static string BuildExpandedLessonTitle(string originalTitle, string segment, int order)
    {
        var firstLine = segment
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        var normalizedTitle = NormalizeBulletTitle(firstLine);

        if (!string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return normalizedTitle;
        }

        return $"{originalTitle} - Phan {order}";
    }

    private static string NormalizeBulletTitle(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        var cleaned = BulletPrefixCleanupRegex().Replace(line.Trim(), string.Empty).Trim();
        return cleaned;
    }

    [GeneratedRegex(@"^(chuong|chương|phan|phần|module|unit|tuan|tuần|week)\s+\d+[\.: -]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ModuleHeadingRegex();

    [GeneratedRegex(@"^(bai|bài|lesson|buoi|buổi|chu de|chủ đề|topic|session)\s+\d+[\.: -]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LessonHeadingRegex();

    [GeneratedRegex(@"^((\d+[\.\)])|([A-Za-z][\.\)])|([IVXLC]+[\.\)])|[-*•])\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BulletOrNumberedLineRegex();

    [GeneratedRegex(@"^((\d+[\.\)])|([A-Za-z][\.\)])|([IVXLC]+[\.\)])|[-*•])\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BulletPrefixCleanupRegex();
}
