using CourseVideo.API.Models;

namespace CourseVideo.API.Services.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetRefreshTokenExpiryUtc();
}
