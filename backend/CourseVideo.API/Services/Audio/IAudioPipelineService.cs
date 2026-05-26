using CourseVideo.API.DTOs.AudioWorker;

namespace CourseVideo.API.Services.Audio;

public interface IAudioPipelineService
{
    Task<AudioWorkerLessonResponse> GenerateLessonAudioAsync(Guid lessonId, List<NarrationSegment> narrationSegments, CancellationToken cancellationToken = default);
}
