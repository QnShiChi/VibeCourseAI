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

    [Fact]
    public async Task GenerateLessonQuizAsync_ThrowsTimeout_WhenResponseBodyNeverCompletes()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new NeverEndingStream())
        });

        var service = CreateService(handler, timeoutSeconds: 1);

        var action = () => service.GenerateLessonQuizAsync(
            new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" },
            new Module { Id = Guid.NewGuid(), Title = "M1", Description = "Desc" },
            new Lesson { Id = Guid.NewGuid(), Title = "L1", Description = "Desc", ContentSeed = "Noi dung bai hoc ve khai niem AI" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await action().WaitAsync(TimeSpan.FromSeconds(2)));

        exception.Message.Should().Be("OpenRouter quiz request timeout.");
    }

    [Fact]
    public async Task GenerateLessonQuizAsync_ThrowsTimeout_WhenResponseBodyIgnoresCancellation()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new NonCooperativeNeverEndingStream())
        });

        var service = CreateService(handler, timeoutSeconds: 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GenerateLessonQuizAsync(
                    new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" },
                    new Module { Id = Guid.NewGuid(), Title = "M1", Description = "Desc" },
                    new Lesson { Id = Guid.NewGuid(), Title = "L1", Description = "Desc", ContentSeed = "Noi dung bai hoc ve khai niem AI" })
                .WaitAsync(TimeSpan.FromSeconds(2)));

        exception.Message.Should().Be("OpenRouter quiz request timeout.");
    }

    private static OpenRouterQuizGenerationService CreateService(HttpMessageHandler handler, int timeoutSeconds = 30)
    {
        var client = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            BaseUrl = "https://openrouter.ai/api/v1",
            TimeoutSeconds = timeoutSeconds
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

    private sealed class NeverEndingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }

    private sealed class NonCooperativeNeverEndingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                await Task.Delay(100);
            }
        }
    }
}
