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

    private static OpenRouterLessonContentService CreateService(HttpMessageHandler handler)
    {
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "openai/gpt-4.1-mini",
            BaseUrl = "https://openrouter.ai/api/v1",
            TimeoutSeconds = 30
        });

        return new OpenRouterLessonContentService(
            new HttpClient(handler),
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

    private static string BuildLessonJson(string lessonId)
    {
        return $$"""
        {
          "lessonId": "{{lessonId}}",
          "lessonTitle": "Cac phuong thuc Generic",
          "teachingScript": "Noi dung bai giang",
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
}
