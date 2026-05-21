using System.Text.Json;

namespace CourseVideo.API.Services;

public static class VoiceoverPlanParser
{
    public static JsonElement Parse(string json)
    {
        VoiceoverPlanValidation.ParseAndValidate(json);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
