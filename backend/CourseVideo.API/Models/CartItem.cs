namespace CourseVideo.API.Models;

public class CartItem : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? GuestCartToken { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
}
