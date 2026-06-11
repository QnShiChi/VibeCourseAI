namespace CourseVideo.API.DTOs.Carts;

public class AddCartItemRequest
{
    public Guid CourseId { get; set; }
    public string? GuestCartToken { get; set; }
}
