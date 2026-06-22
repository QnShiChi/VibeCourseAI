namespace CourseVideo.API.DTOs.Comments;

public class AdminPositiveCourseHighlightResponse
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalCommentCount { get; set; }
    public int PositiveCommentCount { get; set; }
    public double PositiveRatio { get; set; }
    public string LatestPositiveCommentContent { get; set; } = string.Empty;
    public DateTime? LatestPositiveCommentAt { get; set; }
}
