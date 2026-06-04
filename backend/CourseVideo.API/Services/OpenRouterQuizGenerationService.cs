using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services;

public class OpenRouterQuizGenerationService : IOpenRouterQuizGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string VietnameseDiacritics = "ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ";
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterQuizGenerationService> _logger;

    public OpenRouterQuizGenerationService(HttpClient httpClient, IOptions<OpenRouterOptions> options, ILogger<OpenRouterQuizGenerationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<OpenRouterQuizGenerationResult> GenerateLessonQuizAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken = default)
    {
        var questionCount = CalculateLessonQuestionCount(lesson.ContentSeed);
        return GenerateAsync(CreateLessonPrompt(course, module, lesson, questionCount), cancellationToken);
    }

    public Task<OpenRouterQuizGenerationResult> GenerateFinalQuizAsync(Course course, IReadOnlyList<Lesson> lessons, CancellationToken cancellationToken = default)
    {
        var questionCount = CalculateFinalQuestionCount(lessons);
        return GenerateAsync(CreateFinalPrompt(course, lessons, questionCount), cancellationToken);
    }

    private async Task<OpenRouterQuizGenerationResult> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Thiếu cấu hình OPENROUTER_API_KEY.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("Thiếu cấu hình OPENROUTER_MODEL.");
        }

        var requestBody = new OpenRouterChatCompletionRequest
        {
            Model = _options.Model,
            Temperature = 0.1,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "system",
                    Content = """
                    Ban la chuyen gia tao quiz hoc tap bang tieng Viet co dau.
                    Chi tra ve JSON hop le.
                    """
                },
                new OpenRouterMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ],
            ResponseFormat = new OpenRouterResponseFormat
            {
                Type = "json_schema",
                JsonSchema = new OpenRouterJsonSchema
                {
                    Name = "quiz_generation",
                    Strict = true,
                    Schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "title", "questions" },
                        properties = new
                        {
                            title = new { type = "string" },
                            questions = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    required = new[] { "questionText", "explanation", "options" },
                                    properties = new
                                    {
                                        questionText = new { type = "string" },
                                        explanation = new { type = "string" },
                                        options = new
                                        {
                                            type = "array",
                                            items = new
                                            {
                                                type = "object",
                                                additionalProperties = false,
                                                required = new[] { "optionText", "isCorrect" },
                                                properties = new
                                                {
                                                    optionText = new { type = "string" },
                                                    isCorrect = new { type = "boolean" }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 30);

        try
        {
            using var response = await ExecuteWithTimeoutAsync(
                token => _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token),
                timeout,
                cancellationToken,
                () => new InvalidOperationException("OpenRouter quiz request timeout."));

            var payload = await ExecuteWithTimeoutAsync(
                token => response.Content.ReadAsStringAsync(token),
                timeout,
                cancellationToken,
                () => new InvalidOperationException("OpenRouter quiz request timeout."));

            OpenRouterChatCompletionResponse? envelope;

            try
            {
                envelope = JsonSerializer.Deserialize<OpenRouterChatCompletionResponse>(payload, JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("OpenRouter quiz payload không hợp lệ.", exception);
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = string.IsNullOrWhiteSpace(envelope?.Error?.Message)
                    ? $"OpenRouter quiz generation failed with HTTP {(int)response.StatusCode}."
                    : envelope.Error.Message.Trim();

                throw response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new InvalidOperationException(message),
                    _ => new InvalidOperationException(message)
                };
            }

            var content = envelope?.Choices.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("OpenRouter không trả về nội dung quiz.");
            }

            OpenRouterQuizGenerationResult? result;

            try
            {
                result = JsonSerializer.Deserialize<OpenRouterQuizGenerationResult>(content, JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "OpenRouter quiz JSON invalid. Raw content: {Content}", content);
                throw new InvalidOperationException("OpenRouter quiz JSON không hợp lệ.", exception);
            }

            ValidateResult(result);
            return result!;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Không thể kết nối đến OpenRouter để sinh quiz.", exception);
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

    private static void ValidateResult(OpenRouterQuizGenerationResult? result)
    {
        if (result is null || string.IsNullOrWhiteSpace(result.Title) || result.Questions.Count == 0)
        {
            throw new InvalidOperationException("OpenRouter quiz JSON thiếu title hoặc questions.");
        }

        foreach (var question in result.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.QuestionText) ||
                string.IsNullOrWhiteSpace(question.Explanation) ||
                question.Options.Count != 4 ||
                question.Options.Count(option => option.IsCorrect) != 1 ||
                question.Options.Any(option => string.IsNullOrWhiteSpace(option.OptionText)))
            {
                throw new InvalidOperationException("OpenRouter quiz JSON không đúng schema nghiệp vụ.");
            }

            if (!ContainsVietnameseDiacritics(question.QuestionText) ||
                !ContainsVietnameseDiacritics(question.Explanation))
            {
                throw new InvalidOperationException("Quiz phải dùng tiếng Việt có dấu và đúng trọng tâm.");
            }
        }
    }

    private static bool ContainsVietnameseDiacritics(string value)
    {
        return value.Any(character => VietnameseDiacritics.Contains(char.ToLowerInvariant(character)));
    }

    private static int CalculateLessonQuestionCount(string contentSeed)
    {
        var length = contentSeed.Trim().Length;
        if (length < 600)
        {
            return 3;
        }

        if (length < 1800)
        {
            return 5;
        }

        return 7;
    }

    private static int CalculateFinalQuestionCount(IReadOnlyList<Lesson> lessons)
    {
        var totalLength = lessons.Sum(lesson => lesson.ContentSeed?.Length ?? 0);
        if (totalLength < 4000)
        {
            return 10;
        }

        if (totalLength < 12000)
        {
            return 15;
        }

        return 20;
    }

    private static string CreateLessonPrompt(Course course, Module module, Lesson lesson, int questionCount)
    {
        return $"""
        Hay tao quiz cho lesson sau.

        Course:
        - title: {course.Title}
        - description: {course.Description}

        Module:
        - title: {module.Title}
        - description: {module.Description}

        Lesson:
        - title: {lesson.Title}
        - description: {lesson.Description}
        - content: {NormalizeText(lesson.ContentSeed)}

        Yeu cau:
        - Tao dung {questionCount} cau.
        - Toan bo cau hoi, dap an, giai thich phai la tieng Viet co dau.
        - Cau hoi ngan gon, ro nghia, dung trong tam bai hoc.
        - Khong hoi lan man, khong hoi meo vat.
        - Moi cau co dung 4 lua chon.
        - Chi co 1 dap an dung.
        - Giai thich ngan 1-3 cau.
        - Chi tra ve JSON.
        """;
    }

    private static string CreateFinalPrompt(Course course, IReadOnlyList<Lesson> lessons, int questionCount)
    {
        var lessonSummary = string.Join(Environment.NewLine, lessons.Select((lesson, index) =>
            $"- Lesson {index + 1}: {lesson.Title} | {NormalizeText(lesson.ContentSeed)}"));

        return $"""
        Hay tao quiz tong ket cho khoa hoc sau.

        Course:
        - title: {course.Title}
        - description: {course.Description}

        Lessons:
        {lessonSummary}

        Yeu cau:
        - Tao dung {questionCount} cau.
        - Bao quat kien thuc tong quan cua toan khoa hoc.
        - Toan bo cau hoi, dap an, giai thich phai la tieng Viet co dau.
        - Cau hoi ngan gon, ro nghia, dung trong tam.
        - Moi cau co dung 4 lua chon.
        - Chi co 1 dap an dung.
        - Giai thich ngan 1-3 cau.
        - Chi tra ve JSON.
        """;
    }

    private static string NormalizeText(string value)
    {
        return string.Join(' ', value
            .Split(new[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Trim();
    }
}
