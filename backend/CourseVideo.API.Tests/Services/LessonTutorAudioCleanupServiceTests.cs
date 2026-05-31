using CourseVideo.API.Services.Interfaces;
using CourseVideo.API.Services.Tutoring;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonTutorAudioCleanupServiceTests
{
    [Fact]
    public async Task DeleteAssistantAudioAsync_RemovesOnlyAssistantAudioFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"voice-cleanup-{Guid.NewGuid():N}");
        var assistantDir = Path.Combine(root, "storage", "voice-tutor", "assistant-answers");
        var otherDir = Path.Combine(root, "storage", "audio");
        Directory.CreateDirectory(assistantDir);
        Directory.CreateDirectory(otherDir);

        var assistantPath = Path.Combine(assistantDir, "segment.mp3");
        var otherPath = Path.Combine(otherDir, "lesson.mp3");
        await File.WriteAllTextAsync(assistantPath, "assistant");
        await File.WriteAllTextAsync(otherPath, "lesson");

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(root);

        ILessonTutorAudioCleanupService service = new LessonTutorAudioCleanupService(environment.Object);

        await service.DeleteAssistantAudioAsync(
            [
                "/storage/voice-tutor/assistant-answers/segment.mp3",
                "/storage/audio/lesson.mp3"
            ],
            CancellationToken.None);

        File.Exists(assistantPath).Should().BeFalse();
        File.Exists(otherPath).Should().BeTrue();

        Directory.Delete(root, recursive: true);
    }
}
