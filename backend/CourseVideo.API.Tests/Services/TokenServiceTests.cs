using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "System Admin",
            Email = "admin@example.com",
            Role = new Role { Id = 1, Name = "Admin" }
        };

        var token = service.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == userId.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "admin@example.com");
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        jwt.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Name && claim.Value == "System Admin");
    }

    [Fact]
    public void HashRefreshToken_ShouldReturnStableHashForSameInput()
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

        var hash1 = service.HashRefreshToken("refresh-token-value");
        var hash2 = service.HashRefreshToken("refresh-token-value");

        hash1.Should().Be(hash2);
    }
}
