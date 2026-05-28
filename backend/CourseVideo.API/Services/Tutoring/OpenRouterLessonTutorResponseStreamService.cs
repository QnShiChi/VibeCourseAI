using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services.Tutoring;

public class OpenRouterLessonTutorResponseStreamService : ILessonTutorResponseStreamService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxTeachingScriptChars = 900;
    private const int MaxTranscriptChars = 900;
    private const int MaxSlideOutlineChars = 450;
    private const int MaxVoiceoverPlanChars = 450;
    private const int MaxConversationSummaryChars = 280;
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterLessonTutorResponseStreamService(HttpClient httpClient, IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        LessonTutorAnswerRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Missing OPENROUTER_API_KEY for lesson voice tutor.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("Missing OPENROUTER_MODEL for lesson voice tutor.");
        }

        var payload = new OpenRouterChatCompletionRequest
        {
            Model = _options.Model,
            Temperature = 0.1,
            MaxTokens = 110,
            Stream = true,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "system",
                    Content = """
                    Ban la giang vien dang giai thich truc tiep cho nguoi hoc bang tieng Viet.
                    Tra loi suc tich, tu nhien, de nghe nhu mot giang vien that.
                    Mac dinh chi tra loi trong 2 den 4 cau ngan.
                    Khong liet ke dang bullet, khong mo dau kieu chatbot, khong lap lai cau hoi.
                    Uu tien dinh nghia ngan gon, de hieu, neu can thi cho toi da 1 vi du rat ngan.
                    Neu lesson hien tai da du thong tin, khong mo rong them khong can thiet.
                    """
                },
                new OpenRouterMessage
                {
                    Role = "user",
                    Content = $"""
                    Course: {request.Context.CourseTitle}
                    Module: {request.Context.ModuleTitle}
                    Lesson: {request.Context.LessonTitle}
                    Lesson description: {request.Context.LessonDescription}
                    Playback second: {request.Context.PlaybackTimeSeconds}

                    Teaching script:
                    {TrimForPrompt(request.Context.TeachingScript, MaxTeachingScriptChars)}

                    Slide outline:
                    {TrimForPrompt(request.Context.SlideOutlineJson, MaxSlideOutlineChars)}

                    Voiceover plan:
                    {TrimForPrompt(request.Context.VoiceoverPlanJson, MaxVoiceoverPlanChars)}

                    Transcript:
                    {TrimForPrompt(request.Context.TranscriptText, MaxTranscriptChars)}

                    Conversation summary:
                    {TrimForPrompt(request.ConversationSummary ?? string.Empty, MaxConversationSummaryChars)}

                    Learner question:
                    {request.QuestionText}

                    Hay tra loi that ngan, ro, va giong nhu dang noi truc tiep voi sinh vien.
                    """
                }
            ]
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var payloadLine = line["data: ".Length..];
            if (payloadLine == "[DONE]")
            {
                yield break;
            }

            using var document = JsonDocument.Parse(payloadLine);
            var choices = document.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                continue;
            }

            var choice = choices[0];
            if (!choice.TryGetProperty("delta", out var delta)
                || !delta.TryGetProperty("content", out var contentElement))
            {
                continue;
            }

            var content = contentElement.GetString();
            if (!string.IsNullOrWhiteSpace(content))
            {
                yield return content;
            }
        }
    }

    private static string TrimForPrompt(string? value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (normalized.Length <= maxChars)
        {
            return normalized;
        }

        return $"{normalized[..maxChars].Trim()}...";
    }
}
