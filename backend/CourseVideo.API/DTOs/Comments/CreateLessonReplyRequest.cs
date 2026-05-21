namespace CourseVideo.API.DTOs.Comments;

public class CreateLessonReplyRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ReplyToUserId { get; set; }
}
