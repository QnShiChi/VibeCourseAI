using CourseVideo.API.Controllers;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class AdminQuizzesControllerTests
{
    [Fact]
    public async Task Regenerate_ReturnsAccepted()
    {
        var service = new Mock<IQuizGenerationService>();
        service.Setup(x => x.RegenerateQuizAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new AdminQuizzesController(service.Object);

        var result = await controller.Regenerate(Guid.NewGuid());

        result.Should().BeOfType<AcceptedResult>();
    }
}
