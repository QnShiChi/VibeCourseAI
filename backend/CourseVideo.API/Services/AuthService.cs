using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class AuthService : IAuthService
{
    public object Login(LoginRequest request)
    {
        return new
        {
            accessToken = "development-token",
            user = new
            {
                id = Guid.NewGuid(),
                fullName = "System Admin",
                email = request.Email,
                role = "Admin"
            }
        };
    }
}
