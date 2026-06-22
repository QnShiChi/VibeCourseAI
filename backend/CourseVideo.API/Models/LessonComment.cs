namespace CourseVideo.API.Models;

public class LessonComment : BaseEntity
{
    public Guid LessonId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public Guid? ReplyToUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Sentiment { get; set; }
    public DateTime? PinnedAt { get; set; }
    public bool IsHidden { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Lesson? Lesson { get; set; }
    public User? User { get; set; }
    public LessonComment? ParentComment { get; set; }
    public User? ReplyToUser { get; set; }
    public ICollection<LessonComment> Replies { get; set; } = new List<LessonComment>();
    public ICollection<LessonCommentReaction> Reactions { get; set; } = new List<LessonCommentReaction>();
}
