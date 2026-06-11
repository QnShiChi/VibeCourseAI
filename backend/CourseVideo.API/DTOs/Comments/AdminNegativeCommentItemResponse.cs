namespace CourseVideo.API.DTOs.Comments;

public class AdminNegativeCommentItemResponse
{
    public Guid CommentId { get; set; }
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Sentiment { get; set; } = string.Empty;
}
