using System.Threading.Channels;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonVideoJobQueue : ILessonVideoJobQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid jobId)
    {
        if (!_queue.Writer.TryWrite(jobId))
        {
            throw new InvalidOperationException("Không thể đưa video generation job vào hàng đợi.");
        }
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
