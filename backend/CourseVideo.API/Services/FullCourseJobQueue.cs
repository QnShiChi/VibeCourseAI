using System.Threading.Channels;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class FullCourseJobQueue : IFullCourseJobQueue
{
    private readonly Channel<Guid> _queue;

    public FullCourseJobQueue()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Guid>(options);
    }

    public void Enqueue(Guid jobId)
    {
        _queue.Writer.TryWrite(jobId);
    }

    public async Task<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
