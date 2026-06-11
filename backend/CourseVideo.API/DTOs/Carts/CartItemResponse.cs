namespace CourseVideo.API.DTOs.Carts;

public class CartItemResponse
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Price { get; set; }
    public bool AlreadyOwned { get; set; }
}
