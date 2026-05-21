namespace CourseVideo.API.Services.Interfaces;

public interface IGenerationJobQueue
{
    void Enqueue(Guid jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
