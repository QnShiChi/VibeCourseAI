using CourseVideo.API.DTOs.VideoWorker;
using CourseVideo.API.Services.Video;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class RenderServiceTests
{
    [Fact]
    public async Task RenderSlidePngAsync_WhenRemoteImageMissing_RendersLocalIllustrationArea()
    {
        var imageProvider = new FakeImageProvider();
        var service = new RenderService(imageProvider);
        var outputPath = Path.Combine(Path.GetTempPath(), $"render-service-{Guid.NewGuid():N}.png");

        try
        {
            await service.RenderSlidePngAsync(outputPath, new SlideItem
            {
                SlideNumber = 1,
                Title = "Gioi thieu bai hoc",
                ImageKeyword = "artificial intelligence",
                BulletPoints = ["A", "B"],
                SpeakerNotes = "n"
            });

            File.Exists(outputPath).Should().BeTrue();

            using var bitmap = SKBitmap.Decode(outputPath);
            bitmap.Should().NotBeNull();

            var illustrationPixel = bitmap!.GetPixel(1000, 360);
            var panelPixel = bitmap.GetPixel(640, 360);

            illustrationPixel.Should().NotBe(panelPixel);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    private sealed class FakeImageProvider : IImageProvider
    {
        public Task<byte[]?> FetchImageForSlideAsync(string keyword, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<byte[]?>(null);
        }
    }
}
