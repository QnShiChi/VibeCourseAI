using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class AuthService : IAuthService
{
    public LoginResponse Login(LoginRequest request)
    {
        return new LoginResponse
        {
            AccessToken = "development-token",
            User = new AuthUserResponse
            {
                Id = Guid.NewGuid(),
                FullName = "System Admin",
                Email = request.Email,
                Role = "Admin"
            }
        };
    }
}
