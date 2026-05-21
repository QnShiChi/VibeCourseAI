using Microsoft.AspNetCore.Http;

namespace CourseVideo.API.DTOs.Syllabuses;

public class ImportSyllabusRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}
