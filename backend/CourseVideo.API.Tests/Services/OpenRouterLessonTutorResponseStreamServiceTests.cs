using System.Net;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Services.Interfaces;
using CourseVideo.API.Services.Tutoring;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterLessonTutorResponseStreamServiceTests
{
    [Fact]
    public async Task StreamAnswerAsync_YieldsContentTokens_FromSsePayload()
    {
        const string body = """
data: {"choices":[{"delta":{"content":"Xin chao "}}]}

data: {"choices":[{"delta":{"content":"ban."}}]}

data: [DONE]

""";

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
        });

        var client = new HttpClient(handler);
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "key",
            Model = "model",
            BaseUrl = "https://openrouter.ai/api/v1"
        });

        ILessonTutorResponseStreamService service = new OpenRouterLessonTutorResponseStreamService(client, options);

        var chunks = new List<string>();
        await foreach (var chunk in service.StreamAnswerAsync(
            new LessonTutorAnswerRequest(
                new LessonTutorContext("Course", "Module", "Lesson", "Desc", "Script", "[]", "{}", "Transcript", 10),
                "Cau hoi",
                null),
            CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(["Xin chao ", "ban."], chunks);
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
