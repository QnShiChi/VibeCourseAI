using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseResponse>>> GetAll()
    {
        var courses = await _courseService.GetAllAsync();
        return Ok(courses);
    }
}
