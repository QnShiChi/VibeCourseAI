using CourseVideo.API.DTOs.Auth;

namespace CourseVideo.API.Services.Interfaces;

public interface IAuthService
{
    object Login(LoginRequest request);
}
