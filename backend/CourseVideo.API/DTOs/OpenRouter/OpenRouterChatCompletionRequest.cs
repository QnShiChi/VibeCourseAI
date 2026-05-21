using System.Text.Json.Serialization;

namespace CourseVideo.API.DTOs.OpenRouter;

public class OpenRouterChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public IReadOnlyList<OpenRouterMessage> Messages { get; set; } = [];

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("response_format")]
    public OpenRouterResponseFormat? ResponseFormat { get; set; }
}

public class OpenRouterMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OpenRouterResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("json_schema")]
    public OpenRouterJsonSchema? JsonSchema { get; set; }
}

public class OpenRouterJsonSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("strict")]
    public bool Strict { get; set; }

    [JsonPropertyName("schema")]
    public object Schema { get; set; } = new();
}
