using System.Security.Claims;
using CourseVideo.API.DTOs.Carts;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public CartController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<CartResponse>> Get([FromQuery] string? guestCartToken, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.GetCartAsync(GetCurrentUserId(), guestCartToken, cancellationToken));
    }

    [HttpPost("items")]
    [AllowAnonymous]
    public async Task<ActionResult<CartResponse>> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _paymentService.AddCartItemAsync(GetCurrentUserId(), request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("items/{courseId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CartResponse>> RemoveItem(Guid courseId, [FromQuery] string? guestCartToken, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.RemoveCartItemAsync(GetCurrentUserId(), courseId, guestCartToken, cancellationToken));
    }

    [Authorize]
    [HttpPost("merge")]
    public async Task<ActionResult<CartResponse>> Merge([FromBody] MergeCartRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _paymentService.MergeCartAsync(GetRequiredUserId(), request.GuestCartToken, cancellationToken));
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    private Guid GetRequiredUserId()
    {
        return GetCurrentUserId() ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập.");
    }
}
