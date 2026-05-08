namespace CourseVideo.API.DTOs.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public AuthUserResponse User { get; set; } = new();
}
