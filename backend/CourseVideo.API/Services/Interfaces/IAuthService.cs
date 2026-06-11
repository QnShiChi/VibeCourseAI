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
    Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal); // ClaimsPrincipal là đối tượng đại diện cho người dùng hiện tại, chứa thông tin về quyền và danh tính của người dùng
    Task ChangePasswordAsync(Guid currentUserId, ChangePasswordRequest request, string? ipAddress);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, string originUrl, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
