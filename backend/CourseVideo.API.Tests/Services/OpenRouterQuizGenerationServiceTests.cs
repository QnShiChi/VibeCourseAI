using System.Net;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterQuizGenerationServiceTests
{
    [Fact]
    public async Task GenerateLessonQuizAsync_ReturnsParsedQuiz_WhenPayloadIsValid()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "choices": [
                {
                  "message": {
                    "content": "{\"title\":\"Kiem tra nhanh\",\"questions\":[{\"questionText\":\"Khái niệm AI tập trung vào điều gì?\",\"explanation\":\"AI tập trung vào khả năng mô phỏng trí tuệ con người.\",\"options\":[{\"optionText\":\"Mô phỏng trí tuệ con người\",\"isCorrect\":true},{\"optionText\":\"In tài liệu\",\"isCorrect\":false},{\"optionText\":\"Lưu ảnh\",\"isCorrect\":false},{\"optionText\":\"Mở nhạc\",\"isCorrect\":false}]}]}"
                  }
                }
              ]
            }
            """, Encoding.UTF8, "application/json")
        });

        var service = CreateService(handler);

        var result = await service.GenerateLessonQuizAsync(
            new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" },
            new Module { Id = Guid.NewGuid(), Title = "M1", Description = "Desc" },
            new Lesson { Id = Guid.NewGuid(), Title = "L1", Description = "Desc", ContentSeed = "Noi dung bai hoc ve khai niem AI" });

        result.Title.Should().Be("Kiem tra nhanh");
        result.Questions.Should().HaveCount(1);
        result.Questions[0].Options.Should().HaveCount(4);
        result.Questions[0].Options.Should().ContainSingle(x => x.IsCorrect);
    }

    [Fact]
    public async Task GenerateLessonQuizAsync_Throws_WhenPayloadIsNotVietnameseEnough()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "choices": [
                {
                  "message": {
                    "content": "{\"title\":\"Quick quiz\",\"questions\":[{\"questionText\":\"What is AI?\",\"explanation\":\"Because yes.\",\"options\":[{\"optionText\":\"A\",\"isCorrect\":true},{\"optionText\":\"B\",\"isCorrect\":false},{\"optionText\":\"C\",\"isCorrect\":false},{\"optionText\":\"D\",\"isCorrect\":false}]}]}"
                  }
                }
              ]
            }
            """, Encoding.UTF8, "application/json")
        });

        var service = CreateService(handler);

        var action = async () => await service.GenerateLessonQuizAsync(
            new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" },
            new Module { Id = Guid.NewGuid(), Title = "M1", Description = "Desc" },
            new Lesson { Id = Guid.NewGuid(), Title = "L1", Description = "Desc", ContentSeed = "Noi dung bai hoc ve khai niem AI" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tiếng Việt có dấu*");
    }

    private static OpenRouterQuizGenerationService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            BaseUrl = "https://openrouter.ai/api/v1",
            TimeoutSeconds = 30
        });

        return new OpenRouterQuizGenerationService(client, options, NullLogger<OpenRouterQuizGenerationService>.Instance);
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
