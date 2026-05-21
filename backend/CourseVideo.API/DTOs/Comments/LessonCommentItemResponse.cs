namespace CourseVideo.API.DTOs.Comments;

public class LessonCommentItemResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid? ReplyToUserId { get; set; }
    public string? ReplyToUserName { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public bool IsDeleted { get; set; }
    public bool CanDelete { get; set; }
    public bool CanModerate { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<LessonCommentReactionResponse> Reactions { get; set; } = [];
}
