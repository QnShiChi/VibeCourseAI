using CourseVideo.API.DTOs.Modules;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class ModuleServiceTests
{
    [Fact]
    public async Task UpdateAsync_UpdatesModuleMetadata_WhenModuleExists()
    {
        var repository = new Mock<IModuleRepository>();
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Title = "Old title",
            Description = "Old description"
        };

        repository.Setup(x => x.GetByIdAsync(module.Id)).ReturnsAsync(module);
        repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new ModuleService(repository.Object);

        var result = await service.UpdateAsync(module.Id, new UpdateModuleRequest
        {
            Title = "New title",
            Description = "New description"
        });

        result.Should().NotBeNull();
        result!.Title.Should().Be("New title");
        result.Description.Should().Be("New description");
    }
}
