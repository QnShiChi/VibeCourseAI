namespace CourseVideo.API.Models;

public class CourseEnrollment : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid PaymentOrderId { get; set; }
    public PaymentOrder? PaymentOrder { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
