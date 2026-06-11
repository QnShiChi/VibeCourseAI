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
    private readonly IEmailService _emailService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        PasswordHasher<User> passwordHasher,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
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
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return await CreateAuthResponseAsync(user, ipAddress, "User");
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

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, string originUrl, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            // Do not reveal that the user does not exist or is not active
            return;
        }

        var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        user.ResetPasswordToken = Convert.ToBase64String(tokenBytes);
        user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        
        await _userRepository.SaveChangesAsync();

        var encodedToken = Uri.EscapeDataString(user.ResetPasswordToken);
        var encodedEmail = Uri.EscapeDataString(user.Email);
        var resetLink = $"{originUrl}?token={encodedToken}&email={encodedEmail}";

        var htmlBody = $@"
            <h2>Khôi phục mật khẩu</h2>
            <p>Xin chào {user.FullName},</p>
            <p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản VibeCourse AI của bạn.</p>
            <p>Vui lòng click vào đường link bên dưới để đặt lại mật khẩu. Link này sẽ hết hạn trong vòng 15 phút:</p>
            <p><a href='{resetLink}'>{resetLink}</a></p>
            <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
            <br/>
            <p>Trân trọng,<br/>VibeCourse AI Team</p>";

        await _emailService.SendEmailAsync(user.Email, "Khôi phục mật khẩu - VibeCourse AI", htmlBody, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("Yêu cầu không hợp lệ.");
        }

        if (user.ResetPasswordToken != request.Token || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Link khôi phục không hợp lệ hoặc đã hết hạn.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.ResetPasswordToken = null;
        user.ResetPasswordTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();
        await RevokeAllUserSessionsAsync(user.Id, null);
    }

    public Task<AuthResponse> IssueAuthResponseAsync(User user, string? ipAddress)
    {
        return CreateAuthResponseAsync(user, ipAddress);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, string? ipAddress, string? fallbackRoleName = null)
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
                Role = user.Role?.Name ?? fallbackRoleName ?? string.Empty
            }
        };
    }
    
    private Task RevokeAllUserSessionsAsync(Guid userId, string? ipAddress)
    {
        return _refreshTokenRepository.RevokeAllByUserIdAsync(userId, ipAddress);
    }
}
