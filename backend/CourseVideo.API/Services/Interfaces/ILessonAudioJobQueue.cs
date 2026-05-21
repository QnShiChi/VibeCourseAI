namespace CourseVideo.API.Services.Interfaces;

public interface ILessonAudioJobQueue
{
    void Enqueue(Guid jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
