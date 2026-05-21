namespace CourseVideo.API.Services.Interfaces;

public interface ILessonVideoJobQueue
{
    void Enqueue(Guid jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
