using System.Threading.Channels;

namespace CourseVideo.API.Services.Interfaces;

public interface IFullCourseJobQueue
{
    void Enqueue(Guid jobId);
    Task<Guid> DequeueAsync(CancellationToken cancellationToken);
}
