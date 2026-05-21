namespace CourseVideo.API.Models;

public class LessonCommentReaction : BaseEntity
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public LessonComment? Comment { get; set; }
    public User? User { get; set; }
}
