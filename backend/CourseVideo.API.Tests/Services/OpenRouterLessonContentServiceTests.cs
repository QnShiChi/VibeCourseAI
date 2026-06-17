using System.Net;
using System.Text;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterLessonContentServiceTests
{
    [Fact]
    public async Task GenerateAsync_UsesRequestedLessonId_WhenModelReturnsDifferentButValidGuid()
    {
        var lesson = CreateLesson();
        var wrongLessonId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(BuildEnvelope(BuildLessonJson(wrongLessonId.ToString())), Encoding.UTF8, "application/json")
        });

        var service = CreateService(handler);

        var result = await service.GenerateAsync(CreateCourse(lesson), CreateModule(lesson), lesson, CancellationToken.None);

        result.LessonId.Should().Be(lesson.Id);
        result.LessonTitle.Should().Be(lesson.Title);
    }

    [Fact]
    public async Task GenerateAsync_RetriesOnce_WhenFirstResponseContainsInvalidJson()
    {
        var lesson = CreateLesson();
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            var content = attempts == 1
                ? "{\"lessonId\":\"broken\""
                : BuildLessonJson(lesson.Id.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildEnvelope(content), Encoding.UTF8, "application/json")
            };
        });

        var service = CreateService(handler);

        var result = await service.GenerateAsync(CreateCourse(lesson), CreateModule(lesson), lesson, CancellationToken.None);

        attempts.Should().Be(2);
        result.LessonId.Should().Be(lesson.Id);
        result.LessonTitle.Should().Be(lesson.Title);
    }

    [Fact]
    public async Task GenerateAsync_UsesRequestedLessonId_WhenModelReturnsMalformedGuidString()
    {
        var lesson = CreateLesson();
        var malformedLessonId = lesson.Id.ToString()[..30];
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(BuildEnvelope(BuildLessonJson(malformedLessonId)), Encoding.UTF8, "application/json")
        });

        var service = CreateService(handler);

        var result = await service.GenerateAsync(CreateCourse(lesson), CreateModule(lesson), lesson, CancellationToken.None);

        result.LessonId.Should().Be(lesson.Id);
        result.LessonTitle.Should().Be(lesson.Title);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsTimeout_WhenResponseBodyNeverCompletes()
    {
        var lesson = CreateLesson();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new NeverEndingStream())
        });

        var service = CreateService(handler, timeoutSeconds: 1);

        var exception = await Assert.ThrowsAsync<LessonContentGenerationException>(async () =>
            await service.GenerateAsync(CreateCourse(lesson), CreateModule(lesson), lesson, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(2)));

        exception.Message.Should().Be("OpenRouter request timeout.");
    }

    [Fact]
    public async Task GenerateAsync_ThrowsTimeout_WhenResponseBodyIgnoresCancellation()
    {
        var lesson = CreateLesson();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new NonCooperativeNeverEndingStream())
        });

        var service = CreateService(handler, timeoutSeconds: 1);

        var exception = await Assert.ThrowsAsync<LessonContentGenerationException>(async () =>
            await service.GenerateAsync(CreateCourse(lesson), CreateModule(lesson), lesson, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(2)));

        exception.Message.Should().Be("OpenRouter request timeout.");
    }

    [Fact]
    public async Task GenerateAsync_SendsPromptThatForbidsSlideLanguageInTeachingScript()
    {
        var lesson = CreateLesson();
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildEnvelope(BuildLessonJson(lesson.Id.ToString())), Encoding.UTF8, "application/json")
            };
        });

        var service = CreateService(handler);

        await service.GenerateAsync(CreateCourse(lesson), CreateModule(lesson), lesson, CancellationToken.None);

        requestBody.Should().NotBeNull();
        using var document = JsonDocument.Parse(requestBody!);
        var userPrompt = document.RootElement.GetProperty("messages")[1].GetProperty("content").GetString();

        userPrompt.Should().Contain("teachingScript la loi thuyet minh DUY NHAT se duoc doc thanh audio cho video");
        userPrompt.Should().Contain("tuyet doi khong dung cac cum tu nhu: \"slide\"");
    }

    [Fact]
    public async Task GenerateAsync_RetriesWhenTeachingScriptContainsForbiddenSlideLanguage()
    {
        var lesson = CreateLesson();
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            var content = attempts == 1
                ? BuildLessonJson(lesson.Id.ToString(), "Slide này giới thiệu khái niệm cốt lõi của bài học.")
                : BuildLessonJson(lesson.Id.ToString(), "Chúng ta bắt đầu với khái niệm cốt lõi của bài học.");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildEnvelope(content), Encoding.UTF8, "application/json")
            };
        });

        var service = CreateService(handler);

        var result = await service.GenerateAsync(CreateCourse(lesson), CreateModule(lesson), lesson, CancellationToken.None);

        attempts.Should().Be(2);
        result.TeachingScript.Should().Be("Chúng ta bắt đầu với khái niệm cốt lõi của bài học.");
    }

    private static OpenRouterLessonContentService CreateService(HttpMessageHandler handler, int timeoutSeconds = 30)
    {
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "openai/gpt-4.1-mini",
            BaseUrl = "https://openrouter.ai/api/v1",
            TimeoutSeconds = timeoutSeconds
        });

        return new OpenRouterLessonContentService(
            new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            },
            options,
            NullLogger<OpenRouterLessonContentService>.Instance);
    }

    private static Course CreateCourse(Lesson lesson)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            Title = "Lap trinh huong doi tuong",
            Description = "Mo ta khoa hoc",
            SyllabusId = Guid.NewGuid(),
            Modules = [CreateModule(lesson)]
        };
    }

    private static Module CreateModule(Lesson lesson)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Title = "Module Generics",
            Description = "Mo ta module",
            OrderIndex = 1,
            Lessons = [lesson]
        };

        lesson.Module = module;
        return module;
    }

    private static Lesson CreateLesson()
    {
        return new Lesson
        {
            Id = Guid.Parse("751575c9-bafc-45d6-98a7-26849f5a9f17"),
            Title = "Cac phuong thuc Generic",
            Description = "Mo ta lesson",
            ContentSeed = "Seed",
            OrderIndex = 1
        };
    }

    private static string BuildEnvelope(string contentJson)
    {
        return $$"""
        {
          "choices": [
            {
              "message": {
                "content": {{System.Text.Json.JsonSerializer.Serialize(contentJson)}}
              }
            }
          ]
        }
        """;
    }

    private static string BuildLessonJson(string lessonId, string teachingScript = "Noi dung bai giang")
    {
        return $$"""
        {
          "lessonId": "{{lessonId}}",
          "lessonTitle": "Cac phuong thuc Generic",
          "teachingScript": {{System.Text.Json.JsonSerializer.Serialize(teachingScript)}},
          "slideOutline": [
            {
              "slideNumber": 1,
              "title": "Slide 1",
              "bulletPoints": ["Y 1"],
              "speakerNotes": "Ghi chu slide"
            }
          ],
          "voiceoverPlan": {
            "estimatedDurationMinutes": 5,
            "tone": "clear",
            "pacing": "steady",
            "targetAudience": "student",
            "pronunciationNotes": "none"
          }
        }
        """;
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
