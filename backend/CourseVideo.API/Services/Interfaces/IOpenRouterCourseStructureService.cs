namespace CourseVideo.API.Services.Interfaces;

public interface IOpenRouterCourseStructureService
{
    Task<ParsedCourseStructure> GenerateStructureAsync(string extractedText, CancellationToken cancellationToken = default);
}
