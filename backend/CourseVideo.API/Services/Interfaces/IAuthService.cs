using System.Security.Claims;
using CourseVideo.API.DTOs.Auth;

namespace CourseVideo.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress);
    Task LogoutAsync(Guid currentUserId, RefreshTokenRequest request, string? ipAddress);
    Task LogoutAllAsync(Guid currentUserId, string? ipAddress);
    Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal);
    Task ChangePasswordAsync(Guid currentUserId, ChangePasswordRequest request, string? ipAddress);
}
