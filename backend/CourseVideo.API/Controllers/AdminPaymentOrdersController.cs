using CourseVideo.API.Data;
using CourseVideo.API.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/admin/payment-orders")]
[Authorize(Roles = "Admin")]
public class AdminPaymentOrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminPaymentOrdersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentOrders([FromQuery] string? query, [FromQuery] string? status)
    {
        var paymentOrders = await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(paymentOrder => paymentOrder.User)
            .Include(paymentOrder => paymentOrder.Course)
            .ToListAsync();

        var normalizedQuery = query?.Trim();
        var normalizedStatus = status?.Trim();

        var filteredOrders = paymentOrders
            .Where(paymentOrder =>
                string.IsNullOrWhiteSpace(normalizedStatus)
                || string.Equals(paymentOrder.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            .Where(paymentOrder =>
                string.IsNullOrWhiteSpace(normalizedQuery)
                || ContainsIgnoreCase(paymentOrder.OrderCode, normalizedQuery)
                || ContainsIgnoreCase(paymentOrder.User?.FullName, normalizedQuery)
                || ContainsIgnoreCase(paymentOrder.User?.Email, normalizedQuery))
            .OrderByDescending(paymentOrder => paymentOrder.PaidAt ?? paymentOrder.CreatedAt)
            .Select(MapListItem)
            .ToList();

        return Ok(filteredOrders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPaymentOrderDetail(Guid id)
    {
        var paymentOrder = await _dbContext.PaymentOrders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.Course)
            .FirstOrDefaultAsync(order => order.Id == id);

        if (paymentOrder is null)
        {
            return NotFound();
        }

        return Ok(MapDetailItem(paymentOrder));
    }

    private static bool ContainsIgnoreCase(string? source, string query)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static AdminPaymentOrderListItemResponse MapListItem(Models.PaymentOrder paymentOrder)
    {
        return new AdminPaymentOrderListItemResponse
        {
            PaymentOrderId = paymentOrder.Id,
            OrderCode = paymentOrder.OrderCode,
            UserId = paymentOrder.UserId,
            UserFullName = paymentOrder.User?.FullName ?? "Người dùng",
            UserEmail = paymentOrder.User?.Email ?? string.Empty,
            CourseId = paymentOrder.CourseId,
            CourseTitle = paymentOrder.Course?.Title ?? string.Empty,
            Amount = paymentOrder.Amount,
            Status = paymentOrder.Status,
            CreatedAt = paymentOrder.CreatedAt,
            ExpiresAt = paymentOrder.ExpiresAt,
            PaidAt = paymentOrder.PaidAt
        };
    }

    private static AdminPaymentOrderDetailResponse MapDetailItem(Models.PaymentOrder paymentOrder)
    {
        return new AdminPaymentOrderDetailResponse
        {
            PaymentOrderId = paymentOrder.Id,
            OrderCode = paymentOrder.OrderCode,
            UserId = paymentOrder.UserId,
            UserFullName = paymentOrder.User?.FullName ?? "Người dùng",
            UserEmail = paymentOrder.User?.Email ?? string.Empty,
            CourseId = paymentOrder.CourseId,
            CourseTitle = paymentOrder.Course?.Title ?? string.Empty,
            Amount = paymentOrder.Amount,
            Status = paymentOrder.Status,
            CreatedAt = paymentOrder.CreatedAt,
            ExpiresAt = paymentOrder.ExpiresAt,
            PaidAt = paymentOrder.PaidAt,
            BankCode = paymentOrder.BankCode ?? string.Empty,
            BankName = paymentOrder.BankName ?? string.Empty,
            BankAccountNumber = paymentOrder.BankAccountNumber ?? string.Empty,
            AccountHolderName = paymentOrder.AccountHolderName ?? string.Empty,
            TransferContent = paymentOrder.TransferContent,
            SepayTransactionId = paymentOrder.SepayTransactionId
        };
    }
}
