using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;
using CourseVideo.API.Models.OpenRouter;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services;

public class OpenRouterLessonContentService : IOpenRouterLessonContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] ForbiddenTeachingScriptPhrases =
    [
        "slide này",
        "ở slide này",
        "trong slide này",
        "slide tiếp theo",
        "slide cuối cùng",
        "trang chiếu",
        "trên màn hình",
        "nhìn vào"
    ];
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterLessonContentService> _logger;

    public OpenRouterLessonContentService(HttpClient httpClient, IOptions<OpenRouterOptions> options, ILogger<OpenRouterLessonContentService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OpenRouterLessonContentResult> GenerateAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new OpenRouterConfigurationException("Thiếu cấu hình OPENROUTER_API_KEY.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new OpenRouterConfigurationException("Thiếu cấu hình OPENROUTER_MODEL.");
        }

        var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 30);
        LessonContentGenerationException? lastRetryableException = null;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                return await GenerateOnceAsync(course, module, lesson, timeout, cancellationToken);
            }
            catch (LessonContentGenerationException exception) when (attempt < 2 && IsRetryableLessonContentFailure(exception))
            {
                lastRetryableException = exception;
                _logger.LogWarning(
                    exception,
                    "Retrying lesson content generation for lesson {LessonId} after invalid OpenRouter response on attempt {Attempt}.",
                    lesson.Id,
                    attempt);
            }
        }

        throw lastRetryableException ?? new LessonContentGenerationException("Không thể sinh nội dung lesson từ OpenRouter.");
    }

    private async Task<OpenRouterLessonContentResult> GenerateOnceAsync(
        Course course,
        Module module,
        Lesson lesson,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var requestBody = CreateRequest(_options.Model, course, module, lesson);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await ExecuteWithTimeoutAsync(
                token => _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token),
                timeout,
                cancellationToken,
                () => new LessonContentGenerationException("OpenRouter request timeout."));

            var payload = await ExecuteWithTimeoutAsync(
                token => response.Content.ReadAsStringAsync(token),
                timeout,
                cancellationToken,
                () => new LessonContentGenerationException("OpenRouter request timeout."));

            OpenRouterChatCompletionResponse? envelope;

            try
            {
                envelope = JsonSerializer.Deserialize<OpenRouterChatCompletionResponse>(payload, JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new LessonContentGenerationException("OpenRouter trả về payload lesson content không hợp lệ.", exception);
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = string.IsNullOrWhiteSpace(envelope?.Error?.Message)
                    ? $"OpenRouter trả về lỗi HTTP {(int)response.StatusCode}."
                    : envelope!.Error!.Message!.Trim();

                throw response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new OpenRouterConfigurationException(message),
                    _ => new LessonContentGenerationException(message)
                };
            }

            var content = envelope?.Choices.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new LessonContentGenerationException("OpenRouter không trả về nội dung lesson content.");
            }

            var normalizedContent = NormalizeLessonContentJson(content, lesson.Id);
            OpenRouterLessonContentResult? result;

            try
            {
                result = JsonSerializer.Deserialize<OpenRouterLessonContentResult>(normalizedContent, JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Failed to deserialize lesson content JSON for lesson {LessonId}. Raw content: {RawContent}", lesson.Id, TruncateForLog(content));
                throw new LessonContentGenerationException("JSON lesson content từ OpenRouter không hợp lệ.", exception);
            }

            try
            {
                ValidateResult(lesson.Id, result);
            }
            catch (LessonContentGenerationException exception)
            {
                _logger.LogWarning(exception, "Lesson content schema validation failed for lesson {LessonId}. Raw content: {RawContent}", lesson.Id, TruncateForLog(content));
                throw;
            }

            return result!;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (LessonContentGenerationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new LessonContentGenerationException("Không thể kết nối đến OpenRouter để sinh nội dung lesson.", exception);
        }
    }

    private static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operationFactory,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Func<Exception> timeoutExceptionFactory)
    {
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var operationTask = operationFactory(operationCancellation.Token);
        var timeoutTask = Task.Delay(timeout);
        var cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        var completedTask = await Task.WhenAny(operationTask, timeoutTask, cancellationTask);

        if (completedTask == operationTask)
        {
            try
            {
                return await operationTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw timeoutExceptionFactory();
            }
        }

        operationCancellation.Cancel();

        if (completedTask == cancellationTask)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        throw timeoutExceptionFactory();
    }

    private static void ValidateResult(Guid lessonId, OpenRouterLessonContentResult? result)
    {
        if (result is null)
        {
            throw new LessonContentGenerationException("OpenRouter không trả về dữ liệu lesson content.");
        }

        if (result.LessonId != lessonId ||
            string.IsNullOrWhiteSpace(result.LessonTitle) ||
            string.IsNullOrWhiteSpace(result.TeachingScript) ||
            result.SlideOutline.Count == 0 ||
            result.SlideOutline.Any(slide => string.IsNullOrWhiteSpace(slide.Title) ||
                                             slide.BulletPoints.Count == 0 ||
                                             string.IsNullOrWhiteSpace(slide.SpeakerNotes)) ||
            result.VoiceoverPlan.EstimatedDurationMinutes <= 0)
        {
            throw new LessonContentGenerationException("OpenRouter trả về lesson content không đúng schema nghiệp vụ.");
        }

        if (ContainsForbiddenTeachingScriptLanguage(result.TeachingScript))
        {
            throw new LessonContentGenerationException("OpenRouter trả về lesson content không đúng schema nghiệp vụ.");
        }
    }

    private static OpenRouterChatCompletionRequest CreateRequest(string model, Course course, Module module, Lesson lesson)
    {
        var contentSeed = NormalizeContentSeed(lesson.ContentSeed);

        return new OpenRouterChatCompletionRequest
        {
            Model = model,
            Temperature = 0.1,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "system",
                    Content = """
                    Ban la instructional designer va script writer cho bai giang dai hoc bang tieng Viet.
                    Hay tao teaching script, slide outline va voiceover plan cho mot lesson.
                    Chi tra ve JSON dung schema. Khong them giai thich nao ben ngoai JSON.
                    """
                },
                new OpenRouterMessage
                {
                    Role = "user",
                    Content = $"""
                    Hay sinh noi dung cho lesson sau.

                    Course:
                    - id: {course.Id}
                    - title: {course.Title}
                    - description: {course.Description}

                    Module:
                    - id: {module.Id}
                    - title: {module.Title}
                    - description: {module.Description}

                    Lesson:
                    - id: {lesson.Id}
                    - title: {lesson.Title}
                    - description: {lesson.Description}
                    - contentSeed: {contentSeed}

                    Output phai co:
                    - lessonId
                    - lessonTitle
                    - teachingScript
                    - slideOutline[]
                      - slideNumber
                      - title
                      - imageKeyword
                      - bulletPoints[]
                      - speakerNotes
                    - voiceoverPlan
                      - estimatedDurationMinutes
                      - tone
                      - pacing
                      - targetAudience
                      - pronunciationNotes

                    Quy tac:
                    - giu tieng Viet sach, de giang
                    - teachingScript la loi thuyet minh DUY NHAT se duoc doc thanh audio cho video
                    - teachingScript phai chia thanh nhieu doan, moi doan cach nhau bang MOT dong trong, tong so doan phai tuong ung voi tong so phan tu trong slideOutline
                    - moi doan teachingScript phai la loi giang tu nhien, giong giang vien dang noi trong video, khong phai ghi chu dan canh
                    - tuyet doi khong dung cac cum tu nhu: "slide", "trang chieu", "o slide nay", "trong slide nay", "slide tiep theo", "tren man hinh", "nhin vao"
                    - imageKeyword: BẮT BUỘC PHẢI CÓ. 1 câu lệnh (prompt) bằng tiếng Anh miêu tả bức tranh minh họa cho slide. QUAN TRỌNG: AI vẽ tranh không biết viết chữ, nên bạn TUYỆT ĐỐI KHÔNG miêu tả các vật thể có chữ (như: code snippets, screens with text, labeled diagrams, charts, books with text). Hãy miêu tả bằng ẨN DỤ THỊ GIÁC TRỪU TƯỢNG (Abstract visual metaphors). Phải có các từ khóa: "Flat vector illustration, minimalist corporate style, completely abstract, purely visual, NO TEXT, NO NUMBERS, NO WORDS". Ví dụ ĐÚNG: "Flat vector illustration, minimalist corporate style, a glowing evolution path showing interconnected digital nodes, clean aesthetics, NO TEXT, NO NUMBERS, NO WORDS"
                    - slide bullet points ngan gon
                    - speakerNotes phai bo sung dien giai cho slide de ho tro he thong, khong duoc viet nhu loi doc voiceover va khong duoc chep nguyen van teachingScript
                    - teachingScript phai doc duoc thanh tieng noi
                    """
                }
            ],
            ResponseFormat = new OpenRouterResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new OpenRouterJsonSchema
                {
                    Name = "lesson_content",
                    Strict = true,
                    Schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "lessonId", "lessonTitle", "teachingScript", "slideOutline", "voiceoverPlan" },
                        properties = new
                        {
                            lessonId = new { type = "string", format = "uuid" },
                            lessonTitle = new { type = "string" },
                            teachingScript = new { type = "string" },
                            slideOutline = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    required = new[] { "slideNumber", "title", "imageKeyword", "bulletPoints", "speakerNotes" },
                                    properties = new
                                    {
                                        slideNumber = new { type = "integer" },
                                        title = new { type = "string" },
                                        imageKeyword = new { type = "string" },
                                        bulletPoints = new
                                        {
                                            type = "array",
                                            items = new { type = "string" }
                                        },
                                        speakerNotes = new { type = "string" }
                                    }
                                }
                            },
                            voiceoverPlan = new
                            {
                                type = "object",
                                additionalProperties = false,
                                required = new[] { "estimatedDurationMinutes", "tone", "pacing", "targetAudience", "pronunciationNotes" },
                                properties = new
                                {
                                    estimatedDurationMinutes = new { type = "integer" },
                                    tone = new { type = "string" },
                                    pacing = new { type = "string" },
                                    targetAudience = new { type = "string" },
                                    pronunciationNotes = new { type = "string" }
                                }
                            }
                        }
                    }
                }
            }
        };
    }


    private static string NormalizeLessonContentJson(string content, Guid lessonId)
    {
        try
        {
            var jsonNode = JsonNode.Parse(content)?.AsObject();
            if (jsonNode is null)
            {
                return content;
            }

            jsonNode["lessonId"] = lessonId.ToString();
            return jsonNode.ToJsonString();
        }
        catch (JsonException)
        {
            return content;
        }
    }

    private static string TruncateForLog(string value)
    {
        const int maxLength = 2000;
        var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength]}...";
    }

    private static string NormalizeContentSeed(string contentSeed)
    {
        var normalized = contentSeed.Trim();
        const int maxLength = 4000;

        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength].Trim()}...";
    }

    private static bool IsRetryableLessonContentFailure(LessonContentGenerationException exception)
    {
        return exception.Message is
            "JSON lesson content từ OpenRouter không hợp lệ."
            or "OpenRouter trả về lesson content không đúng schema nghiệp vụ."
            or "OpenRouter không trả về dữ liệu lesson content.";
    }

    private static bool ContainsForbiddenTeachingScriptLanguage(string teachingScript)
    {
        var normalized = teachingScript.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return ForbiddenTeachingScriptPhrases.Any(phrase =>
            Regex.IsMatch(
                normalized,
                $@"\b{Regex.Escape(phrase)}\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }
}

public class LessonContentGenerationException : Exception
{
    public LessonContentGenerationException(string message) : base(message)
    {
    }

    public LessonContentGenerationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
