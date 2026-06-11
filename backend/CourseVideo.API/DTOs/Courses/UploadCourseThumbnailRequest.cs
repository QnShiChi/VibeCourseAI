namespace CourseVideo.API.DTOs.Courses;

public class UploadCourseThumbnailRequest
{
    // IFormFile is used to represent a file sent with the HTTP request
    public IFormFile? File { get; set; }
}
