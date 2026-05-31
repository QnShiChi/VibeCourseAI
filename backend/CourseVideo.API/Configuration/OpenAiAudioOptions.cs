namespace CourseVideo.API.Configuration;

public class OpenAiAudioOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string TranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";
}
