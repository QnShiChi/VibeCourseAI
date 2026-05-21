namespace CourseVideo.API.Configuration;

public class OpenRouterOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public int TimeoutSeconds { get; set; } = 30;
}
