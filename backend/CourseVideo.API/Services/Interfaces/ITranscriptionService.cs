namespace CourseVideo.API.Services.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(byte[] audioBytes, CancellationToken cancellationToken);
}

public record TranscriptionResult(string Text, decimal Confidence);
