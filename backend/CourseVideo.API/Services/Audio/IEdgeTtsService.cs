namespace CourseVideo.API.Services.Audio;

public interface IEdgeTtsService
{
    Task<byte[]> SynthesizeToBytesAsync(string text, string? voice = null, CancellationToken cancellationToken = default);
}
