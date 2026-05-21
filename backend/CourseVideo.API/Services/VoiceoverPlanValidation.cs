using System.Text.Json;

namespace CourseVideo.API.Services;

public static class VoiceoverPlanValidation
{
    public static void ParseAndValidate(string json)
    {
        JsonElement root;

        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Voiceover plan JSON không hợp lệ.", exception);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Voiceover plan JSON không hợp lệ.");
        }

        var duration = ReadNumber(root, "estimatedDurationMinutes", "EstimatedDurationMinutes");
        var tone = ReadString(root, "tone", "Tone");
        var pacing = ReadString(root, "pacing", "Pacing");
        var targetAudience = ReadString(root, "targetAudience", "TargetAudience");
        var pronunciationNotes = ReadString(root, "pronunciationNotes", "PronunciationNotes");

        if (duration <= 0 ||
            string.IsNullOrWhiteSpace(tone) ||
            string.IsNullOrWhiteSpace(pacing) ||
            string.IsNullOrWhiteSpace(targetAudience) ||
            string.IsNullOrWhiteSpace(pronunciationNotes))
        {
            throw new InvalidOperationException(
                "Voiceover plan phải có estimatedDurationMinutes, tone, pacing, targetAudience và pronunciationNotes hợp lệ."
            );
        }
    }

    private static double ReadNumber(JsonElement root, string camelCase, string pascalCase)
    {
        if (root.TryGetProperty(camelCase, out var camelValue) && camelValue.TryGetDouble(out var camelNumber))
        {
            return camelNumber;
        }

        if (root.TryGetProperty(pascalCase, out var pascalValue) && pascalValue.TryGetDouble(out var pascalNumber))
        {
            return pascalNumber;
        }

        return 0;
    }

    private static string ReadString(JsonElement root, string camelCase, string pascalCase)
    {
        if (root.TryGetProperty(camelCase, out var camelValue) && camelValue.ValueKind == JsonValueKind.String)
        {
            return camelValue.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty(pascalCase, out var pascalValue) && pascalValue.ValueKind == JsonValueKind.String)
        {
            return pascalValue.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
