using System.Security.Claims;
using CourseVideo.API.DTOs.Payments;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api")]
public class CheckoutController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public CheckoutController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [Authorize]
    [HttpPost("checkout/orders")]
    public async Task<ActionResult<IReadOnlyList<PaymentOrderResponse>>> CreateOrders(
        [FromBody] CreateCheckoutOrdersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _paymentService.CreateOrdersAsync(GetCurrentUserId(), request.CourseIds, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpGet("payment-orders/{id:guid}")]
    public async Task<ActionResult<PaymentOrderResponse>> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var response = await _paymentService.GetOrderAsync(GetCurrentUserId(), id, User.IsInRole("Admin"), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [Authorize]
    [HttpGet("payment-orders")]
    public async Task<ActionResult<IReadOnlyList<PurchaseHistoryItemResponse>>> GetPurchaseHistory(CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetPurchaseHistoryAsync(GetCurrentUserId(), cancellationToken));
    }

    [Authorize]
    [HttpPost("payment-orders/{id:guid}/cancel")]
    public async Task<ActionResult<PaymentOrderResponse>> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _paymentService.CancelOrderAsync(GetCurrentUserId(), id, User.IsInRole("Admin"), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
    }
}
