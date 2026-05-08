using CourseVideo.API.DTOs.Auth;

namespace CourseVideo.API.Services.Interfaces;

public interface IAuthService
{   
    LoginResponse Login(LoginRequest request);
}
