using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.OpenRouter;

public class OpenRouterChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<OpenRouterChoice> Choices { get; set; } = [];

    [JsonPropertyName("error")]
    public OpenRouterError? Error { get; set; }
}

public class OpenRouterChoice
{
    [JsonPropertyName("message")]
    public OpenRouterAssistantMessage? Message { get; set; }
}

public class OpenRouterAssistantMessage
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OpenRouterError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
