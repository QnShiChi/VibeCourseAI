using System.Net;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterCourseStructureServiceTests
{
    [Fact]
    public async Task GenerateStructureAsync_ReturnsValidatedStructure_WhenAiJsonIsValid()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{\"courseTitle\":\"Lap trinh huong doi tuong\",\"courseDescription\":\"Mo ta khoa hoc\",\"modules\":[{\"title\":\"Chuong 1\",\"description\":\"Tong quan\",\"lessons\":[{\"title\":\"Bai 1\",\"description\":\"Mo dau\",\"contentSeed\":\"Hat giong noi dung\"}]}]}"
                      }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var service = CreateService(handler);

        var result = await service.GenerateStructureAsync("Noi dung de cuong");

        result.CourseTitle.Should().Be("Lap trinh huong doi tuong");
        result.CourseDescription.Should().Be("Mo ta khoa hoc");
        result.Modules.Should().ContainSingle();
        result.Modules[0].Lessons.Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateStructureAsync_ThrowsValidationException_WhenModulesAreEmpty()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{\"courseTitle\":\"OOP\",\"courseDescription\":\"Mo ta\",\"modules\":[]}"
                      }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var service = CreateService(handler);
        var action = async () => await service.GenerateStructureAsync("Noi dung de cuong");

        await action.Should().ThrowAsync<OpenRouterValidationException>();
    }

    [Fact]
    public async Task GenerateStructureAsync_ThrowsValidationException_WhenLessonMissingContentSeed()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{\"courseTitle\":\"OOP\",\"courseDescription\":\"Mo ta\",\"modules\":[{\"title\":\"Chuong 1\",\"description\":\"Tong quan\",\"lessons\":[{\"title\":\"Bai 1\",\"description\":\"Mo dau\",\"contentSeed\":\"\"}]}]}"
                      }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var service = CreateService(handler);
        var action = async () => await service.GenerateStructureAsync("Noi dung de cuong");

        await action.Should().ThrowAsync<OpenRouterValidationException>();
    }

    [Fact]
    public async Task GenerateStructureAsync_ThrowsValidationException_WhenJsonIsMalformed()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{invalid json"
                      }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        var service = CreateService(handler);
        var action = async () => await service.GenerateStructureAsync("Noi dung de cuong");

        await action.Should().ThrowAsync<OpenRouterValidationException>();
    }

    [Fact]
    public async Task GenerateStructureAsync_ThrowsTechnicalException_WhenHttpFails()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":{\"message\":\"rate limited\"}}", Encoding.UTF8, "application/json")
        });

        var service = CreateService(handler);
        var action = async () => await service.GenerateStructureAsync("Noi dung de cuong");

        await action.Should().ThrowAsync<OpenRouterTechnicalException>();
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
