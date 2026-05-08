using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> GetAllAsync();
}
