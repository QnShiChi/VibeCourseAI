namespace CourseVideo.API.Services.Interfaces;

public interface ICourseStructureParser
{
    ParsedCourseStructure Parse(string extractedText);
}

public class ParsedCourseStructure
{
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public List<ParsedModuleStructure> Modules { get; set; } = [];
}

public class ParsedModuleStructure
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ParsedLessonStructure> Lessons { get; set; } = [];
}

public class ParsedLessonStructure
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentSeed { get; set; } = string.Empty;
}
