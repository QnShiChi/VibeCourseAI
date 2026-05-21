namespace CourseVideo.API.DTOs.Comments;

public class LessonCommentListResponse
{
    public IReadOnlyList<LessonCommentThreadResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
    public string Sort { get; set; } = "newest";
}
