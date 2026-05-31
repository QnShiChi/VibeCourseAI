namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorAudioCleanupService
{
    Task DeleteAssistantAudioAsync(IEnumerable<string> audioUrls, CancellationToken cancellationToken);
}
