using CourseVideo.API.DTOs.Carts;
using CourseVideo.API.DTOs.Payments;

namespace CourseVideo.API.Services.Interfaces;

public interface IPaymentService
{
    Task<CartResponse> GetCartAsync(Guid? userId, string? guestCartToken, CancellationToken cancellationToken = default);
    Task<CartResponse> AddCartItemAsync(Guid? userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartResponse> RemoveCartItemAsync(Guid? userId, Guid courseId, string? guestCartToken, CancellationToken cancellationToken = default);
    Task<CartResponse> MergeCartAsync(Guid userId, string guestCartToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentOrderResponse>> CreateOrdersAsync(Guid userId, IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default);
    Task<PaymentOrderResponse?> GetOrderAsync(Guid userId, Guid orderId, bool isAdmin, CancellationToken cancellationToken = default);
    Task HandleSepayWebhookAsync(
        SepayWebhookPayload payload,
        string rawPayload,
        string? apiKeyHeader,
        CancellationToken cancellationToken = default,
        bool validateWebhookCredential = true);
    Task<bool> HasCourseAccessAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);
}
