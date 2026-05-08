using CourseVideo.API.DTOs.Courses;

namespace CourseVideo.API.Services.Interfaces;

public interface ICourseService
{
    Task<IReadOnlyList<CourseResponse>> GetAllAsync();
}
