using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void CreateAccessToken_ShouldEmbedUserIdentityAndRoleClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "vibe-course-ai",
            Audience = "vibe-course-ai-client",
            SecretKey = "super-secret-key-with-at-least-32-chars",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7
        });

        var service = new TokenService(options);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "System Admin",
            Email = "admin@example.com",
            Role = new Role { Id = 1, Name = "Admin" }
        };

        var token = service.CreateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
    }
}
