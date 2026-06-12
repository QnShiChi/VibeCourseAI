using CourseVideo.API.Services.Video;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class StorageServiceTests
{
    [Fact]
    public void GetStorageDirectory_UsesContentRootStorageDirectory()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns("/tmp/vibecourse");

        var service = new StorageService(environment.Object);

        service.GetStorageDirectory().Should().Be(Path.Combine("/tmp/vibecourse", "storage"));
    }
}
