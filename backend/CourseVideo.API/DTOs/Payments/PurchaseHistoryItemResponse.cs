namespace CourseVideo.API.DTOs.Payments;

public class PurchaseHistoryItemResponse
{
    public Guid PaymentOrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? CourseThumbnailUrl { get; set; }
    public int Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
