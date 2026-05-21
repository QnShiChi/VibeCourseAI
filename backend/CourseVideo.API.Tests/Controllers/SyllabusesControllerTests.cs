using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.GenerationJobs;
using CourseVideo.API.DTOs.Syllabuses;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class SyllabusesControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithEmptyList_WhenNoSyllabusesExist()
    {
        var service = new Mock<ISyllabusService>();
        var generationService = new Mock<ICourseGenerationService>();
        service.Setup(x => x.GetAllAsync()).ReturnsAsync(Array.Empty<SyllabusListItemResponse>());

        var controller = new SyllabusesController(service.Object, generationService.Object);

        var result = await controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(Array.Empty<SyllabusListItemResponse>());
    }

    [Fact]
    public async Task Import_ShouldReturnBadRequest_WhenFileMissing()
    {
        var service = new Mock<ISyllabusService>();
        var generationService = new Mock<ICourseGenerationService>();
        var controller = new SyllabusesController(service.Object, generationService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateAdminContext()
            }
        };

        var result = await controller.Import(new ImportSyllabusRequest
        {
            Title = "Web",
            Description = "Mo ta",
            File = null
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Generate_ReturnsOk_WhenGenerationSucceeds()
    {
        var service = new Mock<ISyllabusService>();
        var generationService = new Mock<ICourseGenerationService>();
        var syllabusId = Guid.NewGuid();
        generationService.Setup(x => x.GenerateFromSyllabusAsync(syllabusId, It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new GenerateCourseResponse
            {
                JobId = Guid.NewGuid(),
                Status = "Completed",
                SyllabusId = syllabusId,
                CourseId = Guid.NewGuid(),
                CourseTitle = "Khoa hoc OOP",
                CreatedAt = DateTime.UtcNow
            });

        var controller = new SyllabusesController(service.Object, generationService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateAdminContext()
            }
        };

        var result = await controller.Generate(syllabusId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<GenerateCourseResponse>();
    }

    [Fact]
    public async Task Generate_ReturnsConflict_WhenRunningJobExists()
    {
        var service = new Mock<ISyllabusService>();
        var generationService = new Mock<ICourseGenerationService>();
        var syllabusId = Guid.NewGuid();
        generationService.Setup(x => x.GenerateFromSyllabusAsync(syllabusId, It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Đề cương này đang có job generate đang chạy."));

        var controller = new SyllabusesController(service.Object, generationService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateAdminContext()
            }
        };

        var result = await controller.Generate(syllabusId);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Generate_ReturnsBadRequest_WithSpecificAiMessage()
    {
        var service = new Mock<ISyllabusService>();
        var generationService = new Mock<ICourseGenerationService>();
        var syllabusId = Guid.NewGuid();
        generationService.Setup(x => x.GenerateFromSyllabusAsync(syllabusId, It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Thiếu cấu hình OPENROUTER_API_KEY."));

        var controller = new SyllabusesController(service.Object, generationService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateAdminContext()
            }
        };

        var result = await controller.Generate(syllabusId);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new { message = "Thiếu cấu hình OPENROUTER_API_KEY." });
    }

    private static DefaultHttpContext CreateAdminContext()
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Name, "Admin User")
            }, "Test"))
        };
    }
}
