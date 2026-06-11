using System.Net;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterCourseStructureDensityTests
{
    [Fact]
    public async Task GenerateStructureAsync_ThrowsValidationException_WhenLongSyllabusProducesSparseLessons()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{\"courseTitle\":\"OOP\",\"courseDescription\":\"Mo ta\",\"modules\":[{\"title\":\"Chuong 1\",\"description\":\"Tong quan\",\"lessons\":[{\"title\":\"Bai 1\",\"description\":\"Mo dau\",\"contentSeed\":\"Hat giong 1\"}]},{\"title\":\"Chuong 2\",\"description\":\"Thuc hanh\",\"lessons\":[{\"title\":\"Bai 2\",\"description\":\"Ap dung\",\"contentSeed\":\"Hat giong 2\"}]}]}"
                      }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var service = CreateService(handler);
        var longSyllabus = string.Join('\n', Enumerable.Range(1, 80).Select(index => $"Tuan {index}: Noi dung hoc tap chi tiet va bai tap mo rong"));
        var action = async () => await service.GenerateStructureAsync(longSyllabus);

        await action.Should().ThrowAsync<OpenRouterValidationException>()
            .WithMessage("*qua it lesson*");
    }

    private static OpenRouterCourseStructureService CreateService(HttpMessageHandler handler)
    {
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "openai/gpt-4.1-mini",
            BaseUrl = "https://openrouter.ai/api/v1",
            TimeoutSeconds = 30
        });

        return new OpenRouterCourseStructureService(
            new HttpClient(handler),
            options,
            new OpenRouterPromptFactory());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
