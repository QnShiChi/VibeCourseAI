namespace CourseVideo.API.DTOs.Comments;

public class LessonCommentReactionResponse
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool ReactedByCurrentUser { get; set; }
}
