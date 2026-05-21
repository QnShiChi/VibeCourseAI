namespace CourseVideo.API.DTOs.Comments;

public class LessonCommentThreadResponse
{
    public LessonCommentItemResponse Comment { get; set; } = new();
    public IReadOnlyList<LessonCommentItemResponse> Replies { get; set; } = [];
}
