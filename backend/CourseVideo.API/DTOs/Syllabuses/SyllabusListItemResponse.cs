namespace CourseVideo.API.DTOs.Syllabuses;

public class SyllabusListItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
}
