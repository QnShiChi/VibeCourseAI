namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorResponseStreamService
{
    IAsyncEnumerable<string> StreamAnswerAsync(
        LessonTutorAnswerRequest request,
        CancellationToken cancellationToken);
}
