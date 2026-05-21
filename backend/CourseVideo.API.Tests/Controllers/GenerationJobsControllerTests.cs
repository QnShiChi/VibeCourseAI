using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.GenerationJobs;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class GenerationJobsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOk_WithJobs()
    {
        var service = new Mock<ICourseGenerationService>();
        service.Setup(x => x.GetAllJobsAsync()).ReturnsAsync(new[]
        {
            new GenerationJobListItemResponse
            {
                Id = Guid.NewGuid(),
                SyllabusId = Guid.NewGuid(),
                SyllabusTitle = "Lap trinh huong doi tuong",
                Status = "Completed"
            }
        });
        var controller = new GenerationJobsController(service.Object);

        var result = await controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<GenerationJobListItemResponse>>();
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenJobMissing()
    {
        var service = new Mock<ICourseGenerationService>();
        service.Setup(x => x.GetJobByIdAsync(It.IsAny<Guid>())).ReturnsAsync((GenerationJobDetailResponse?)null);
        var controller = new GenerationJobsController(service.Object);

        var result = await controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }
}
