namespace CourseVideo.API.Services.Interfaces;

public interface ILessonTutorSegmenter
{
    IReadOnlyList<string> PushText(string text);
    IReadOnlyList<string> FlushRemaining();
}
