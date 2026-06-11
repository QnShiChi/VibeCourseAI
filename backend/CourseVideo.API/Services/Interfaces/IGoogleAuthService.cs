using CourseVideo.API.DTOs.Auth;

namespace CourseVideo.API.Services.Interfaces;

public interface IGoogleAuthService
{
    string BuildAuthorizationUrl(string backendCallbackUrl);
    Task<string> HandleCallbackAsync(
        string code,
        string state,
        string backendCallbackUrl,
        string? error,
        string? errorDescription,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<AuthResponse> ExchangeAsync(string exchangeToken, CancellationToken cancellationToken = default);
}
