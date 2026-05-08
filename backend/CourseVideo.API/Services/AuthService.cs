using System.Security.Claims;
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace CourseVideo.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        PasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        if (await _userRepository.GetByEmailAsync(request.Email) is not null)
        {
            throw new InvalidOperationException("Email đã tồn tại.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            RoleId = 2,
            Role = new Role { Id = 2, Name = "User" },
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Tài khoản đã bị khóa.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        return await CreateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash)
            ?? throw new UnauthorizedAccessException("Refresh token không hợp lệ.");

        if (storedToken.RevokedAt is not null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
        }

        if (storedToken.User is null || !storedToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.UpdatedAt = DateTime.UtcNow;

        var nextRefreshToken = _tokenService.CreateRefreshToken();
        storedToken.ReplacedByTokenHash = _tokenService.HashRefreshToken(nextRefreshToken);

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = storedToken.ReplacedByTokenHash,
            ExpiresAt = _tokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = ipAddress
        });

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = _tokenService.CreateAccessToken(storedToken.User),
            RefreshToken = nextRefreshToken,
            User = new AuthUserResponse
            {
                Id = storedToken.User.Id,
                FullName = storedToken.User.FullName,
                Email = storedToken.User.Email,
                Role = storedToken.User.Role?.Name ?? string.Empty
            }
        };
    }

    public async Task LogoutAsync(Guid currentUserId, RefreshTokenRequest request, string? ipAddress)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken is null || storedToken.UserId != currentUserId)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.UpdatedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();
    }

    public Task LogoutAllAsync(Guid currentUserId, string? ipAddress)
    {
        return RevokeAllUserSessionsAsync(currentUserId, ipAddress);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");

        var userId = Guid.Parse(subject);
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("Không tìm thấy người dùng.");

        return new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role?.Name ?? string.Empty,
            IsActive = user.IsActive
        };
    }

    public async Task ChangePasswordAsync(Guid currentUserId, ChangePasswordRequest request, string? ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(currentUserId)
            ?? throw new UnauthorizedAccessException("Không tìm thấy người dùng.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
        await RevokeAllUserSessionsAsync(user.Id, ipAddress);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, string? ipAddress)
    {
        var refreshToken = _tokenService.CreateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = _tokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = ipAddress
        });
        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = _tokenService.CreateAccessToken(user),
            RefreshToken = refreshToken,
            User = new AuthUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role?.Name ?? string.Empty
            }
        };
    }

    private Task RevokeAllUserSessionsAsync(Guid userId, string? ipAddress)
    {
        return _refreshTokenRepository.RevokeAllByUserIdAsync(userId, ipAddress);
    }
}
