using System.Security.Claims;
using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.Users;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task UpdateActive_ShouldReturnNoContent_WhenServiceCompletes()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Learner",
            Email = "learner@example.com",
            RoleId = 2,
            IsActive = false
        };

        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        userRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(repository => repository.RevokeAllByUserIdAsync(user.Id, It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        refreshTokenRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var controller = new UsersController(userRepository.Object, refreshTokenRepository.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") },
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "Test"))
                }
            }
        };

        var result = await controller.UpdateActive(user.Id, new UpdateUserActiveRequest { IsActive = false });

        result.Should().BeOfType<NoContentResult>();
    }
}
