using System.Text;
using CourseVideo.API.DTOs.Syllabuses;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class SyllabusServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldStoreTxtFileAndExtractText()
    {
        var repository = new Mock<ISyllabusRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<Syllabus>())).Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(tempRoot);

        try
        {
            var service = new SyllabusService(repository.Object, environment.Object);
            var bytes = Encoding.UTF8.GetBytes("De cuong mon hoc");
            await using var stream = new MemoryStream(bytes);
            IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "syllabus.txt");

            var response = await service.ImportAsync(
                new ImportSyllabusRequest
                {
                    Title = "Web",
                    Description = "Mo ta",
                    File = file
                },
                Guid.NewGuid(),
                "Admin User");

            response.ExtractedText.Should().Contain("De cuong mon hoc");
            response.FileType.Should().Be("txt");
            repository.Verify(x => x.AddAsync(It.Is<Syllabus>(s => s.Title == "Web" && s.ExtractedText.Contains("De cuong mon hoc"))), Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task ImportAsync_ShouldRejectUnsupportedExtension()
    {
        var repository = new Mock<ISyllabusRepository>();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());
        var service = new SyllabusService(repository.Object, environment.Object);

        var bytes = Encoding.UTF8.GetBytes("malicious");
        await using var stream = new MemoryStream(bytes);
        IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "syllabus.exe");

        var action = async () => await service.ImportAsync(
            new ImportSyllabusRequest { Title = "Bad", Description = "Nope", File = file },
            Guid.NewGuid(),
            "Admin User");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Chỉ hỗ trợ file pdf, docx hoặc txt.");
    }
}
