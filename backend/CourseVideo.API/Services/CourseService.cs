using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<IReadOnlyList<CourseResponse>> GetAllAsync()
    {
        var courses = await _courseRepository.GetAllAsync();
        return courses.Select(course => new CourseResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt
        }).ToList();
    }
}
