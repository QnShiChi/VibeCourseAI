using System.Net.Http.Headers;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services.Transcription;

public class OpenAiTranscriptionService : ITranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiAudioOptions _options;

    public OpenAiTranscriptionService(HttpClient httpClient, IOptions<OpenAiAudioOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<TranscriptionResult> TranscribeAsync(byte[] audioBytes, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Missing OPENAI_API_KEY for lesson voice transcription.");
        }

        using var form = new MultipartFormDataContent();
        using var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
        form.Add(audioContent, "file", "question.webm");
        form.Add(new StringContent(_options.TranscriptionModel), "model");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/audio/transcriptions")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(payload);
        var text = document.RootElement.GetProperty("text").GetString() ?? string.Empty;
        return new TranscriptionResult(text.Trim(), 1m);
    }
}
