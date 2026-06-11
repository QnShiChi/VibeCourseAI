namespace CourseVideo.API.Configuration;

public class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string FrontendCallbackUrl { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
    public string UserInfoEndpoint { get; set; } = "https://openidconnect.googleapis.com/v1/userinfo";
}
