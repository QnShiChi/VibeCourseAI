using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services;

public class OpenRouterCourseStructureService : IOpenRouterCourseStructureService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly OpenRouterPromptFactory _promptFactory;

    public OpenRouterCourseStructureService(
        HttpClient httpClient,
        IOptions<OpenRouterOptions> options,
        OpenRouterPromptFactory promptFactory)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _promptFactory = promptFactory;
    }

    public async Task<ParsedCourseStructure> GenerateStructureAsync(string extractedText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new OpenRouterConfigurationException("Thiếu cấu hình OPENROUTER_API_KEY.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new OpenRouterConfigurationException("Thiếu cấu hình OPENROUTER_MODEL.");
        }

        var requestBody = _promptFactory.Create(_options.Model, extractedText);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException exception)
        {
            throw new OpenRouterTechnicalException("OpenRouter request timeout.", exception);
        }
        catch (Exception exception)
        {
            throw new OpenRouterTechnicalException("Không thể kết nối đến OpenRouter.", exception);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        OpenRouterChatCompletionResponse? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<OpenRouterChatCompletionResponse>(payload, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new OpenRouterValidationException("OpenRouter trả về payload không hợp lệ.", exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = envelope?.Error?.Message;
            throw CreateExceptionFromStatusCode(response.StatusCode, errorMessage);
        }

        var content = envelope?.Choices.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new OpenRouterValidationException("OpenRouter không trả về nội dung cấu trúc khóa học.");
        }

        ParsedCourseStructure? structure;

        try
        {
            structure = JsonSerializer.Deserialize<ParsedCourseStructure>(content, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new OpenRouterValidationException("JSON cấu trúc khóa học từ OpenRouter không hợp lệ.", exception);
        }

        ValidateStructure(structure);
        return structure!;
    }

    private static void ValidateStructure(ParsedCourseStructure? structure)
    {
        if (structure is null)
        {
            throw new OpenRouterValidationException("OpenRouter không trả về dữ liệu cấu trúc khóa học.");
        }

        if (string.IsNullOrWhiteSpace(structure.CourseTitle) || string.IsNullOrWhiteSpace(structure.CourseDescription))
        {
            throw new OpenRouterValidationException("OpenRouter trả về course title/description không hợp lệ.");
        }

        if (structure.Modules.Count == 0)
        {
            throw new OpenRouterValidationException("OpenRouter không sinh ra module nào.");
        }

        foreach (var module in structure.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Title) || string.IsNullOrWhiteSpace(module.Description))
            {
                throw new OpenRouterValidationException("OpenRouter trả về module không hợp lệ.");
            }

            if (module.Lessons.Count == 0)
            {
                throw new OpenRouterValidationException("OpenRouter trả về module không có lesson.");
            }

            foreach (var lesson in module.Lessons)
            {
                if (string.IsNullOrWhiteSpace(lesson.Title) ||
                    string.IsNullOrWhiteSpace(lesson.Description) ||
                    string.IsNullOrWhiteSpace(lesson.ContentSeed))
                {
                    throw new OpenRouterValidationException("OpenRouter trả về lesson không hợp lệ.");
                }
            }
        }
    }

    private static Exception CreateExceptionFromStatusCode(HttpStatusCode statusCode, string? message)
    {
        var resolvedMessage = string.IsNullOrWhiteSpace(message)
            ? $"OpenRouter trả về lỗi HTTP {(int)statusCode}."
            : message.Trim();

        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                => new OpenRouterConfigurationException(resolvedMessage),
            HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout
                => new OpenRouterTechnicalException(resolvedMessage),
            _ => new OpenRouterTechnicalException(resolvedMessage)
        };
    }
}

public class OpenRouterConfigurationException : Exception
{
    public OpenRouterConfigurationException(string message) : base(message)
    {
    }
}

public class OpenRouterTechnicalException : Exception
{
    public OpenRouterTechnicalException(string message) : base(message)
    {
    }

    public OpenRouterTechnicalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class OpenRouterValidationException : Exception
{
    public OpenRouterValidationException(string message) : base(message)
    {
    }

    public OpenRouterValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
