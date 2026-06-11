namespace CourseVideo.API.DTOs.Payments;

public class CreateCheckoutOrdersRequest
{
    public IReadOnlyList<Guid> CourseIds { get; set; } = [];
}
