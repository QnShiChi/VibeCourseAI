namespace CourseVideo.API.Services.Audio;

public interface IEdgeTtsService
{
    Task<byte[]> SynthesizeToBytesAsync(string text, CancellationToken cancellationToken = default);
}
