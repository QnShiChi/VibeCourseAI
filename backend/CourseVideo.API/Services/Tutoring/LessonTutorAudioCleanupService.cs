using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services.Tutoring;

public class LessonTutorAudioCleanupService : ILessonTutorAudioCleanupService
{
    private const string AssistantAudioPrefix = "/storage/voice-tutor/assistant-answers/";
    private readonly IWebHostEnvironment _environment;

    public LessonTutorAudioCleanupService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task DeleteAssistantAudioAsync(IEnumerable<string> audioUrls, CancellationToken cancellationToken)
    {
        var baseDirectory = Path.Combine(_environment.ContentRootPath, "storage", "voice-tutor", "assistant-answers");

        foreach (var audioUrl in audioUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct(StringComparer.Ordinal))
        {
            if (!audioUrl.StartsWith(AssistantAudioPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var fileName = Path.GetFileName(audioUrl);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var path = Path.Combine(baseDirectory, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        return Task.CompletedTask;
    }
}
