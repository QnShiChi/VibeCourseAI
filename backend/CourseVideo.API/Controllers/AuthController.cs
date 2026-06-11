using System.Security.Claims;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly GoogleAuthOptions _googleAuthOptions;

    public AuthController(IAuthService authService, IGoogleAuthService googleAuthService, IOptions<GoogleAuthOptions> googleAuthOptions)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
        _googleAuthOptions = googleAuthOptions.Value;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
        await _authService.LogoutAsync(currentUserId, request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
        await _authService.LogoutAllAsync(currentUserId, HttpContext.Connection.RemoteIpAddress?.ToString());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _authService.GetCurrentUserAsync(User);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("me/payment-profile")]
    public async Task<IActionResult> UpdateAdminPaymentProfile([FromBody] UpdateAdminPaymentProfileRequest request)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
            var result = await _authService.UpdateAdminPaymentProfileAsync(currentUserId, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
            await _authService.ChangePasswordAsync(currentUserId, request, HttpContext.Connection.RemoteIpAddress?.ToString());
            return NoContent();
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var originUrl = Request.Headers["Origin"].FirstOrDefault() ?? "http://localhost:3000";
        var resetUrl = $"{originUrl.TrimEnd('/')}/reset-password";
        
        await _authService.ForgotPasswordAsync(request, resetUrl);
        return Ok(new { message = "Nếu email hợp lệ, hướng dẫn khôi phục mật khẩu sẽ được gửi đến hòm thư của bạn." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("google/login")]
    public IActionResult GoogleLogin()
    {
        return Redirect(_googleAuthService.BuildAuthorizationUrl(BuildBackendGoogleCallbackUrl()));
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken = default)
    {
        var frontendCallbackUrl = BuildFrontendGoogleCallbackUrl();

        try
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                throw new InvalidOperationException("google_auth_failed");
            }

            var exchangeToken = await _googleAuthService.HandleCallbackAsync(
                code,
                state,
                BuildBackendGoogleCallbackUrl(),
                error,
                errorDescription,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Redirect($"{frontendCallbackUrl}?exchangeToken={Uri.EscapeDataString(exchangeToken)}");
        }
        catch (InvalidOperationException exception)
        {
            return Redirect($"{frontendCallbackUrl}?error={Uri.EscapeDataString(exception.Message)}");
        }
    }

    [HttpPost("google/exchange")]
    public async Task<IActionResult> GoogleExchange([FromBody] GoogleExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExchangeToken))
        {
            return BadRequest(new { message = "Exchange token is required." });
        }

        try
        {
            var result = await _googleAuthService.ExchangeAsync(request.ExchangeToken, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    private string BuildBackendGoogleCallbackUrl()
    {
        return Url.ActionLink(nameof(GoogleCallback), "Auth", null, Request.Scheme, Request.Host.ToString())
            ?? $"{Request.Scheme}://{Request.Host}/api/auth/google/callback";
    }

    private string BuildFrontendGoogleCallbackUrl()
    {
        if (!string.IsNullOrWhiteSpace(_googleAuthOptions.FrontendCallbackUrl))
        {
            return _googleAuthOptions.FrontendCallbackUrl;
        }

        var originUrl = Request.Headers["Origin"].FirstOrDefault() ?? "http://localhost:3000";
        return $"{originUrl.TrimEnd('/')}/auth/google/callback";
    }
}
