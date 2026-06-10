namespace CourseVideo.API.DTOs.Carts;

public class CartResponse
{
    public string GuestCartToken { get; set; } = string.Empty;
    public IReadOnlyList<CartItemResponse> Items { get; set; } = [];
}
