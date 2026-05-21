using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.DTOs.Modules;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class CoursesControllerTests
{
    [Fact]
    public async Task GetStructure_ReturnsOk_WhenCourseExists()
    {
        var courseService = new Mock<ICourseService>();
        var moduleService = new Mock<IModuleService>();
        var lessonService = new Mock<ILessonService>();
        var response = new CourseStructureResponse
        {
            Id = Guid.NewGuid(),
            Title = "Course"
        };

        courseService.Setup(x => x.GetStructureAsync(response.Id)).ReturnsAsync(response);
        var controller = new CoursesController(courseService.Object, moduleService.Object, lessonService.Object);

        var result = await controller.GetStructure(response.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task UpdateModule_ReturnsBadRequest_WhenPayloadMissing()
    {
        var controller = new CoursesController(
            Mock.Of<ICourseService>(),
            Mock.Of<IModuleService>(),
            Mock.Of<ILessonService>());

        var result = await controller.UpdateModule(Guid.NewGuid(), new UpdateModuleRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateLesson_ReturnsBadRequest_WhenPayloadMissing()
    {
        var controller = new CoursesController(
            Mock.Of<ICourseService>(),
            Mock.Of<IModuleService>(),
            Mock.Of<ILessonService>());

        var result = await controller.UpdateLesson(Guid.NewGuid(), new UpdateLessonRequest());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAdminCourses_ReturnsOk_WhenAdminRequestsAllCourses()
    {
        var courseService = new Mock<ICourseService>();
        courseService.Setup(x => x.GetAdminCoursesAsync()).ReturnsAsync(new List<AdminCourseListItemResponse>
        {
            new() { Id = Guid.NewGuid(), Title = "Draft", IsPublished = false, ModuleCount = 1, LessonCount = 2 }
        });
        var controller = CreateAdminController(courseService);

        var result = await controller.GetAdminCourses();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<AdminCourseListItemResponse>>();
    }

    [Fact]
    public async Task GetPublishedCourses_ReturnsOk()
    {
        var courseService = new Mock<ICourseService>();
        courseService.Setup(x => x.GetPublishedCoursesAsync()).ReturnsAsync(new List<PublishedCourseListItemResponse>
        {
            new() { Id = Guid.NewGuid(), Title = "Published", IsPublished = true }
        });
        var controller = CreateUserController(courseService);

        var result = await controller.GetPublishedCourses();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<PublishedCourseListItemResponse>>();
    }

    [Fact]
    public async Task Publish_ReturnsNoContent_WhenCourseExists()
    {
        var courseService = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        courseService.Setup(x => x.PublishAsync(courseId))
            .ReturnsAsync(new AdminCourseListItemResponse { Id = courseId, IsPublished = true });
        var controller = CreateAdminController(courseService);

        var result = await controller.Publish(courseId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetLearn_ReturnsNotFound_WhenDraftPreviewForbiddenForUser()
    {
        var courseService = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        courseService.Setup(x => x.GetLearnPayloadAsync(courseId, false))
            .ReturnsAsync((CourseLearnResponse?)null);
        var controller = CreateUserController(courseService);

        var result = await controller.GetLearn(courseId);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetLearn_ReturnsOk_WhenAdminPreviewsDraftCourse()
    {
        var courseService = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        courseService.Setup(x => x.GetLearnPayloadAsync(courseId, true))
            .ReturnsAsync(new CourseLearnResponse
            {
                CourseId = courseId,
                CourseTitle = "Draft Course",
                SelectedLessonId = Guid.NewGuid(),
                Modules = []
            });
        var controller = CreateAdminController(courseService);

        var result = await controller.GetLearn(courseId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<CourseLearnResponse>();
    }

    [Fact]
    public async Task GenerateLessonContent_ReturnsOk_WhenAdminStartsWholeCourseGeneration()
    {
        var courseService = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        courseService.Setup(x => x.GenerateLessonContentAsync(courseId, It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(new GenerateLessonContentResponse
            {
                JobId = Guid.NewGuid(),
                CourseId = courseId,
                Status = "Pending",
                TotalLessons = 8
            });
        var controller = CreateAdminController(courseService);

        var result = await controller.GenerateLessonContent(courseId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<GenerateLessonContentResponse>();
    }

    [Fact]
    public async Task RegenerateLessonContent_ReturnsOk_WhenAdminRestartsFailedLessonGeneration()
    {
        var courseService = new Mock<ICourseService>();
        var courseId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        courseService.Setup(x => x.RegenerateLessonContentAsync(courseId, lessonId, It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(new GenerateLessonContentResponse
            {
                JobId = Guid.NewGuid(),
                CourseId = courseId,
                Status = "Pending",
                TotalLessons = 1
            });
        var controller = CreateAdminController(courseService);

        var result = await controller.RegenerateLessonContent(courseId, lessonId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<GenerateLessonContentResponse>();
    }

    private static CoursesController CreateAdminController(Mock<ICourseService> courseService)
    {
        return new CoursesController(courseService.Object, Mock.Of<IModuleService>(), Mock.Of<ILessonService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Role, "Admin")
                    ], "Test"))
                }
            }
        };
    }

    private static CoursesController CreateUserController(Mock<ICourseService> courseService)
    {
        return new CoursesController(courseService.Object, Mock.Of<IModuleService>(), Mock.Of<ILessonService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Role, "User")
                    ], "Test"))
                }
            }
        };
    }
}
