using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldCreateUserWithUserRoleAndReturnTokens()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByEmailAsync("new-user@example.com"))
            .ReturnsAsync((User?)null);

        userRepository.Setup(repository => repository.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        userRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        refreshTokenRepository.Setup(repository => repository.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.CreateAccessToken(It.IsAny<User>()))
            .Returns("access-token");
        tokenService.Setup(service => service.CreateRefreshToken())
            .Returns("refresh-token");
        tokenService.Setup(service => service.HashRefreshToken("refresh-token"))
            .Returns("refresh-token-hash");
        tokenService.Setup(service => service.GetRefreshTokenExpiryUtc())
            .Returns(DateTime.UtcNow.AddDays(7));

        var authService = new AuthService(
            userRepository.Object,
            refreshTokenRepository.Object,
            tokenService.Object,
            new PasswordHasher<User>(),
            Mock.Of<IEmailService>());

        var response = await authService.RegisterAsync(new RegisterRequest
        {
            FullName = "New User",
            Email = "new-user@example.com",
            Password = "ChangeMe@123"
        }, "127.0.0.1");

        userRepository.Verify(
            repository => repository.AddAsync(It.Is<User>(user =>
                user.RoleId == 2 &&
                user.Role == null &&
                user.Email == "new-user@example.com")),
            Times.Once);
        response.AccessToken.Should().Be("access-token");
        response.RefreshToken.Should().Be("refresh-token");
        response.User.Role.Should().Be("User");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordIsInvalid()
    {
        var seededUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Existing User",
            Email = "existing@example.com",
            Role = new Role { Id = 2, Name = "User" },
            RoleId = 2
        };

        var passwordHasher = new PasswordHasher<User>();
        seededUser.PasswordHash = passwordHasher.HashPassword(seededUser, "CorrectPassword@123");

        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByEmailAsync("existing@example.com"))
            .ReturnsAsync(seededUser);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var tokenService = new Mock<ITokenService>();

        var authService = new AuthService(
            userRepository.Object,
            refreshTokenRepository.Object,
            tokenService.Object,
            passwordHasher,
            Mock.Of<IEmailService>());

        var action = async () => await authService.LoginAsync(new LoginRequest
        {
            Email = "existing@example.com",
            Password = "WrongPassword@123"
        }, "127.0.0.1");

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
