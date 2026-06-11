using System.Text.Json;
using CourseVideo.API.DTOs.Payments;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/payments/sepay")]
public class SepayWebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public SepayWebhookController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        try
        {
            var payload = JsonSerializer.Deserialize<SepayWebhookPayload>(rawBody, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("Payload SePay không hợp lệ.");

            var authHeader = Request.Headers["Authorization"].FirstOrDefault()
                ?? Request.Headers["X-Secret-Key"].FirstOrDefault();

            await _paymentService.HandleSepayWebhookAsync(payload, rawBody, authHeader, cancellationToken);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { success = false });
        }
        catch
        {
            return Ok(new { success = true });
        }
    }
}
