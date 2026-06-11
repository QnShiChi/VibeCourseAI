using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services.Tutoring;

public class OpenRouterLessonTutorAnswerService : ILessonTutorAnswerService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterLessonTutorAnswerService(HttpClient httpClient, IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<LessonTutorAnswerResult> GenerateAnswerAsync(LessonTutorAnswerRequest request, CancellationToken cancellationToken)
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
            Temperature = 0.2,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "system",
                    Content = """
                    Ban la tro giang giong noi cho mot bai hoc video bang tieng Viet.
                    Uu tien giai thich theo lesson hien tai, mo rong trong course hien tai, va chi dung kien thuc ben ngoai khi lesson/course khong du.
                    Neu co mo rong kien thuc ben ngoai, hay noi ro do la phan bo sung.
                    Tra loi ngan gon, de doc thanh giong noi.
                    Ket thuc bang mot cau ngan hoi xem nguoi hoc co muon giai thich them khong.
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
                    {request.Context.TeachingScript}

                    Slide outline:
                    {request.Context.SlideOutlineJson}

                    Transcript:
                    {request.Context.TranscriptText}

                    Conversation summary:
                    {request.ConversationSummary ?? string.Empty}

                    Learner question:
                    {request.QuestionText}

                    Tra ve JSON co 2 truong: answerText va sourceType. sourceType chi duoc la Lesson, Course, ExternalKnowledge, hoac Mixed.
                    """
                }
            ],
            ResponseFormat = new OpenRouterResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new OpenRouterJsonSchema
                {
                    Name = "lesson_voice_answer",
                    Strict = true,
                    Schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "answerText", "sourceType" },
                        properties = new
                        {
                            answerText = new { type = "string" },
                            sourceType = new
                            {
                                type = "string",
                                @enum = new[] { "Lesson", "Course", "ExternalKnowledge", "Mixed" }
                            }
                        }
                    }
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var envelope = JsonSerializer.Deserialize<OpenRouterChatCompletionResponse>(body, JsonOptions);
        var content = envelope?.Choices.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("OpenRouter returned an empty lesson voice answer.");
        }

        using var doc = JsonDocument.Parse(content);
        return new LessonTutorAnswerResult(
            doc.RootElement.GetProperty("answerText").GetString()?.Trim() ?? string.Empty,
            doc.RootElement.GetProperty("sourceType").GetString()?.Trim() ?? "Mixed");
    }
}
