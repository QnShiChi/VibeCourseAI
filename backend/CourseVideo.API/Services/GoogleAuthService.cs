using System.Net.Http.Headers;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.Auth;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Google;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleAuthOptions _options;
    private readonly GoogleOAuthStateStore _stateStore;
    private readonly GoogleAuthExchangeStore _exchangeStore;
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly PasswordHasher<User> _passwordHasher;

    public GoogleAuthService(
        HttpClient httpClient,
        IOptions<GoogleAuthOptions> options,
        GoogleOAuthStateStore stateStore,
        GoogleAuthExchangeStore exchangeStore,
        IUserRepository userRepository,
        IAuthService authService,
        PasswordHasher<User> passwordHasher)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _stateStore = stateStore;
        _exchangeStore = exchangeStore;
        _userRepository = userRepository;
        _authService = authService;
        _passwordHasher = passwordHasher;
    }

    public string BuildAuthorizationUrl(string backendCallbackUrl)
    {
        var state = _stateStore.Create();
        return QueryHelpers.AddQueryString(_options.AuthorizationEndpoint, new Dictionary<string, string?> // Sử dụng QueryHelpers để xây dựng URL với các tham số truy vấn cần thiết cho quá trình xác thực OAuth2 với Google
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = backendCallbackUrl,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["access_type"] = "online",
            ["prompt"] = "select_account"
        });
    }

    public async Task<string> HandleCallbackAsync(
        string code,
        string state,
        string backendCallbackUrl,
        string? error,
        string? errorDescription,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!_stateStore.Consume(state))
        {
            throw new InvalidOperationException("google_state_invalid");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException("google_auth_failed");
        }

        var googleAccessToken = await ExchangeCodeForAccessTokenAsync(code, backendCallbackUrl, cancellationToken);
        var userInfo = await GetGoogleUserInfoAsync(googleAccessToken, cancellationToken);

        if (string.IsNullOrWhiteSpace(userInfo.Email) || !userInfo.EmailVerified)
        {
            throw new InvalidOperationException("google_email_unverified");
        }

        var user = await _userRepository.GetByEmailAsync(userInfo.Email);
        if (user is null)
        {
            user = new User
            {
                FullName = string.IsNullOrWhiteSpace(userInfo.Name) ? userInfo.Email : userInfo.Name,
                Email = userInfo.Email,
                AvatarUrl = string.IsNullOrWhiteSpace(userInfo.Picture) ? null : userInfo.Picture,
                RoleId = 2,
                IsActive = true
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }
        else
        {
            if (!user.IsActive)
            {
                throw new InvalidOperationException("account_locked");
            }

            if (!string.IsNullOrWhiteSpace(userInfo.Picture) && user.AvatarUrl != userInfo.Picture)
            {
                user.AvatarUrl = userInfo.Picture;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.SaveChangesAsync();
            }
        }

        var authUser = await _userRepository.GetByEmailAsync(userInfo.Email) ?? user;
        var authResponse = await _authService.IssueAuthResponseAsync(authUser, ipAddress);
        return _exchangeStore.Store(authResponse);
    }
    // Cho phép client lấy kết quả xác thực (token, thông tin người dùng) bằng cách sử dụng exchange token mà server đã tạo ra sau khi xử lý callback từ Google, nếu exchange token hợp lệ thì trả về AuthResponse chứa token và thông tin người dùng, nếu không hợp lệ thì ném ra lỗi UnauthorizedAccessException
    public Task<AuthResponse> ExchangeAsync(string exchangeToken, CancellationToken cancellationToken = default)
    {
        var response = _exchangeStore.Take(exchangeToken);
        if (response is null)
        {
            throw new UnauthorizedAccessException("Invalid exchange token.");
        }

        return Task.FromResult(response);
    }

    private async Task<string> ExchangeCodeForAccessTokenAsync(string code, string backendCallbackUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = backendCallbackUrl,
                ["grant_type"] = "authorization_code"
            }!)
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("google_auth_failed");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);

        if (!document.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            throw new InvalidOperationException("google_auth_failed");
        }

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("google_auth_failed");
        }

        return accessToken;
    }

    private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("google_auth_failed");
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return userInfo ?? throw new InvalidOperationException("google_auth_failed");
    }
}
