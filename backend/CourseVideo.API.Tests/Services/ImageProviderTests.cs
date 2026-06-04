using System.Net;
using CourseVideo.API.Services.Video;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class ImageProviderTests
{
    [Fact]
    public async Task FetchImageForSlideAsync_WithoutConfiguredProviders_DoesNotCallNetworkAndReturnsNull()
    {
        var handler = new CountingHandler();
        var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var service = new ImageProvider(httpClient, configuration, NullLogger<ImageProvider>.Instance);

        var result = await service.FetchImageForSlideAsync("artificial intelligence");

        result.Should().BeNull();
        handler.CallCount.Should().Be(0);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3, 4])
            });
        }
    }
}
