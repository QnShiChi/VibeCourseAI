using CourseVideo.API.Controllers;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        var authService = new Mock<IAuthService>();
        var googleAuthService = new Mock<IGoogleAuthService>();
        authService.Setup(service => service.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("Email đã tồn tại."));

        var controller = new AuthController(
            authService.Object,
            googleAuthService.Object,
            Options.Create(new GoogleAuthOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Register(new RegisterRequest
        {
            FullName = "Người dùng",
            Email = "existing@example.com",
            Password = "ChangeMe@123"
        });

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new { message = "Email đã tồn tại." });
    }
}
