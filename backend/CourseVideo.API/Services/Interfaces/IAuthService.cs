using System.Security.Claims;
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Models;

namespace CourseVideo.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress); 
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress); 
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress);
    Task LogoutAsync(Guid currentUserId, RefreshTokenRequest request, string? ipAddress);
    Task LogoutAllAsync(Guid currentUserId, string? ipAddress);
    Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal);
    Task<CurrentUserResponse> UpdateAdminPaymentProfileAsync(Guid currentUserId, UpdateAdminPaymentProfileRequest request);
    Task ChangePasswordAsync(Guid currentUserId, ChangePasswordRequest request, string? ipAddress);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, string originUrl, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> IssueAuthResponseAsync(User user, string? ipAddress);
}
