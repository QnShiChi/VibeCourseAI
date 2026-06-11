# Google Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add backend-managed Google OAuth login that auto-creates `User` accounts, reuses the existing JWT/refresh-token session model, and completes login through a dedicated frontend callback page.

**Architecture:** The backend starts and completes the Google OAuth redirect flow, validates callback state, exchanges the authorization code for Google profile data, resolves or creates the local user, then issues a short-lived one-time exchange token instead of returning app tokens in the redirect. The frontend login page starts the flow, the callback page exchanges the one-time token for the normal `AuthResponse`, and `AuthContext` stores the session exactly like password login.

**Tech Stack:** ASP.NET Core 8 Web API, Entity Framework Core, existing `TokenService`/JWT auth, React 18, React Router 6, Axios, Vitest, Testing Library

---

## File Structure

- Modify: `backend/CourseVideo.API/Program.cs`
  Registers Google auth configuration, HTTP clients, memory cache, and new Google auth services.
- Create: `backend/CourseVideo.API/Configuration/GoogleAuthOptions.cs`
  Holds Google OAuth-related configuration.
- Create: `backend/CourseVideo.API/DTOs/Auth/GoogleExchangeRequest.cs`
  Request payload for one-time exchange token redemption.
- Create: `backend/CourseVideo.API/Services/Interfaces/IGoogleAuthService.cs`
  Contract for Google auth flow orchestration.
- Create: `backend/CourseVideo.API/Services/GoogleAuthService.cs`
  Implements OAuth URL generation, state validation, token exchange, user resolution/creation, and one-time exchange token issuance.
- Create: `backend/CourseVideo.API/Services/Google/GoogleOAuthStateStore.cs`
  Stores short-lived OAuth `state` values.
- Create: `backend/CourseVideo.API/Services/Google/GoogleAuthExchangeStore.cs`
  Stores one-time exchange tokens mapped to `AuthResponse`.
- Create: `backend/CourseVideo.API/Services/Google/GoogleUserInfo.cs`
  DTO for Google user info payload.
- Modify: `backend/CourseVideo.API/Controllers/AuthController.cs`
  Adds Google login start, callback, and exchange endpoints.
- Modify: `backend/CourseVideo.API/Services/AuthService.cs`
  Extracts or reuses token issuance logic for Google flow.
- Modify: `backend/CourseVideo.API/Services/Interfaces/IAuthService.cs`
  If needed, expose a helper for issuing `AuthResponse` from an existing `User`.
- Modify: `backend/CourseVideo.API/Models/User.cs`
  No schema change expected; only touched if helper comments or behavior require it.
- Modify: `.env.example`
  Adds Google auth config placeholders.
- Create: `backend/CourseVideo.API.Tests/Controllers/AuthControllerGoogleTests.cs`
  Tests controller behavior around redirect, callback, and exchange.
- Create: `backend/CourseVideo.API.Tests/Services/GoogleAuthServiceTests.cs`
  Tests Google auth service behavior and exchange-token single-use semantics.
- Modify: `frontend/src/components/auth/AuthShell.jsx`
  Makes Google button start the real backend OAuth flow.
- Modify: `frontend/src/pages/LoginPage.jsx`
  Handles optional login error message coming back from Google callback flow.
- Create: `frontend/src/pages/GoogleAuthCallbackPage.jsx`
  Reads `exchangeToken`, redeems it, stores session, and redirects.
- Modify: `frontend/src/routes/AppRoutes.jsx`
  Registers `/auth/google/callback`.
- Modify: `frontend/src/auth/authService.js`
  Adds exchange API helper.
- Modify: `frontend/src/auth/AuthContext.jsx`
  Adds helper to persist session from Google exchange result.
- Create: `frontend/src/pages/GoogleAuthCallbackPage.test.jsx`
  Tests callback success and failure behavior.
- Modify: `frontend/src/pages/LoginPage.test.jsx`
  Tests Google button behavior or login error propagation.

### Task 1: Add Backend Configuration and Service Boundaries

**Files:**
- Create: `backend/CourseVideo.API/Configuration/GoogleAuthOptions.cs`
- Create: `backend/CourseVideo.API/DTOs/Auth/GoogleExchangeRequest.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/IGoogleAuthService.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `.env.example`
- Test: `backend/CourseVideo.API.Tests/Controllers/AuthControllerGoogleTests.cs`

- [ ] **Step 1: Write the failing controller bootstrap test**

```csharp
[Fact]
public async Task GoogleExchange_ReturnsBadRequest_WhenExchangeTokenMissing()
{
    var googleAuthService = new Mock<IGoogleAuthService>();
    var controller = new AuthController(Mock.Of<IAuthService>(), googleAuthService.Object);

    var result = await controller.GoogleExchange(new GoogleExchangeRequest
    {
        ExchangeToken = ""
    });

    result.Should().BeOfType<BadRequestObjectResult>();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter GoogleExchange_ReturnsBadRequest_WhenExchangeTokenMissing`
Expected: FAIL because `IGoogleAuthService`, `GoogleExchangeRequest`, or controller endpoint do not exist yet

- [ ] **Step 3: Add config and service contracts**

```csharp
namespace CourseVideo.API.Configuration;

public class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string FrontendCallbackUrl { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
    public string UserInfoEndpoint { get; set; } = "https://openidconnect.googleapis.com/v1/userinfo";
}
```

```csharp
namespace CourseVideo.API.DTOs.Auth;

public class GoogleExchangeRequest
{
    public string ExchangeToken { get; set; } = string.Empty;
}
```

```csharp
using CourseVideo.API.DTOs.Auth;

namespace CourseVideo.API.Services.Interfaces;

public interface IGoogleAuthService
{
    string BuildAuthorizationUrl();
    Task<string> HandleCallbackAsync(string code, string state, string? error, string? errorDescription, CancellationToken cancellationToken = default);
    Task<AuthResponse> ExchangeAsync(string exchangeToken, CancellationToken cancellationToken = default);
}
```

```csharp
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();
```

```env
GoogleAuth__ClientId=your_google_client_id
GoogleAuth__ClientSecret=your_google_client_secret
GoogleAuth__FrontendCallbackUrl=http://localhost:3000/auth/google/callback
```

- [ ] **Step 4: Add minimal controller injection and validation**

```csharp
private readonly IGoogleAuthService _googleAuthService;

public AuthController(IAuthService authService, IGoogleAuthService googleAuthService)
{
    _authService = authService;
    _googleAuthService = googleAuthService;
}

[HttpPost("google/exchange")]
public async Task<IActionResult> GoogleExchange([FromBody] GoogleExchangeRequest request)
{
    if (string.IsNullOrWhiteSpace(request.ExchangeToken))
    {
        return BadRequest(new { message = "Exchange token is required." });
    }

    var result = await _googleAuthService.ExchangeAsync(request.ExchangeToken);
    return Ok(result);
}
```

- [ ] **Step 5: Run focused backend build**

Run: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add .env.example backend/CourseVideo.API/Configuration/GoogleAuthOptions.cs backend/CourseVideo.API/DTOs/Auth/GoogleExchangeRequest.cs backend/CourseVideo.API/Services/Interfaces/IGoogleAuthService.cs backend/CourseVideo.API/Program.cs backend/CourseVideo.API/Controllers/AuthController.cs backend/CourseVideo.API.Tests/Controllers/AuthControllerGoogleTests.cs
git commit -m "feat: scaffold google auth configuration"
```

### Task 2: Implement OAuth State and Exchange Token Stores

**Files:**
- Create: `backend/CourseVideo.API/Services/Google/GoogleOAuthStateStore.cs`
- Create: `backend/CourseVideo.API/Services/Google/GoogleAuthExchangeStore.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/GoogleAuthServiceTests.cs`

- [ ] **Step 1: Write the failing exchange-token single-use test**

```csharp
[Fact]
public async Task ExchangeAsync_ConsumesExchangeTokenOnlyOnce()
{
    var store = new GoogleAuthExchangeStore(new MemoryCache(new MemoryCacheOptions()));
    var authResponse = new AuthResponse { AccessToken = "a", RefreshToken = "r", User = new AuthUserResponse { Email = "x@y.com" } };
    var token = store.Store(authResponse);

    var first = store.Take(token);
    var second = store.Take(token);

    first.Should().NotBeNull();
    second.Should().BeNull();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter ExchangeAsync_ConsumesExchangeTokenOnlyOnce`
Expected: FAIL because `GoogleAuthExchangeStore` does not exist

- [ ] **Step 3: Implement the in-memory stores**

```csharp
public class GoogleOAuthStateStore
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

    public GoogleOAuthStateStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Create()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set($"google-oauth-state:{state}", true, Expiry);
        return state;
    }

    public bool Consume(string state)
    {
        var key = $"google-oauth-state:{state}";
        if (!_cache.TryGetValue(key, out _))
        {
            return false;
        }

        _cache.Remove(key);
        return true;
    }
}
```

```csharp
public class GoogleAuthExchangeStore
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(2);

    public GoogleAuthExchangeStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Store(AuthResponse response)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set($"google-auth-exchange:{token}", response, Expiry);
        return token;
    }

    public AuthResponse? Take(string token)
    {
        var key = $"google-auth-exchange:{token}";
        if (!_cache.TryGetValue<AuthResponse>(key, out var response))
        {
            return null;
        }

        _cache.Remove(key);
        return response;
    }
}
```

```csharp
builder.Services.AddSingleton<GoogleOAuthStateStore>();
builder.Services.AddSingleton<GoogleAuthExchangeStore>();
```

- [ ] **Step 4: Run focused backend build**

Run: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Services/Google/GoogleOAuthStateStore.cs backend/CourseVideo.API/Services/Google/GoogleAuthExchangeStore.cs backend/CourseVideo.API/Program.cs backend/CourseVideo.API.Tests/Services/GoogleAuthServiceTests.cs
git commit -m "feat: add google oauth state and exchange stores"
```

### Task 3: Implement Google OAuth Service

**Files:**
- Create: `backend/CourseVideo.API/Services/Google/GoogleUserInfo.cs`
- Create: `backend/CourseVideo.API/Services/GoogleAuthService.cs`
- Modify: `backend/CourseVideo.API/Services/AuthService.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/IAuthService.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/GoogleAuthServiceTests.cs`

- [ ] **Step 1: Write the failing user-resolution test**

```csharp
[Fact]
public async Task HandleCallbackAsync_CreatesNewUser_WhenGoogleEmailDoesNotExist()
{
    var userRepository = new Mock<IUserRepository>();
    userRepository.Setup(x => x.GetByEmailAsync("newuser@example.com")).ReturnsAsync((User?)null);

    var service = BuildGoogleAuthService(userRepository: userRepository.Object, googleUser: new GoogleUserInfo
    {
        Email = "newuser@example.com",
        EmailVerified = true,
        Name = "New User",
        Picture = "https://avatar.example.com/u.png"
    });

    var exchangeToken = await service.HandleCallbackAsync("code", "state", null, null);

    exchangeToken.Should().NotBeNullOrWhiteSpace();
    userRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == "newuser@example.com" && u.RoleId == 2 && u.IsActive)), Times.Once);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter HandleCallbackAsync_CreatesNewUser_WhenGoogleEmailDoesNotExist`
Expected: FAIL because `GoogleAuthService` and `GoogleUserInfo` do not exist

- [ ] **Step 3: Add the Google user DTO and service skeleton**

```csharp
public class GoogleUserInfo
{
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
}
```

```csharp
public class GoogleAuthService : IGoogleAuthService
{
    // inject IOptions<GoogleAuthOptions>, GoogleOAuthStateStore, GoogleAuthExchangeStore,
    // IUserRepository, IAuthService, PasswordHasher<User>, HttpClient
}
```

- [ ] **Step 4: Implement authorization URL, callback, and exchange**

```csharp
public string BuildAuthorizationUrl()
{
    var state = _stateStore.Create();
    var query = QueryString.Create(new Dictionary<string, string?>
    {
        ["client_id"] = _options.ClientId,
        ["redirect_uri"] = _backendCallbackUrl,
        ["response_type"] = "code",
        ["scope"] = "openid email profile",
        ["state"] = state
    });

    return $"{_options.AuthorizationEndpoint}{query}";
}

public async Task<string> HandleCallbackAsync(string code, string state, string? error, string? errorDescription, CancellationToken cancellationToken = default)
{
    if (!_stateStore.Consume(state))
    {
        throw new InvalidOperationException("google_state_invalid");
    }

    if (!string.IsNullOrWhiteSpace(error))
    {
        throw new InvalidOperationException("google_auth_failed");
    }

    var userInfo = await GetGoogleUserInfoAsync(code, cancellationToken);
    if (string.IsNullOrWhiteSpace(userInfo.Email) || !userInfo.EmailVerified)
    {
        throw new InvalidOperationException("google_email_unverified");
    }

    var user = await _userRepository.GetByEmailAsync(userInfo.Email);
    if (user is null)
    {
        user = new User
        {
            FullName = userInfo.Name,
            Email = userInfo.Email,
            AvatarUrl = string.IsNullOrWhiteSpace(userInfo.Picture) ? null : userInfo.Picture,
            RoleId = 2,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
    }
    else if (!user.IsActive)
    {
        throw new InvalidOperationException("account_locked");
    }

    var authResponse = await _authService.IssueAuthResponseAsync(user, null);
    return _exchangeStore.Store(authResponse);
}

public Task<AuthResponse> ExchangeAsync(string exchangeToken, CancellationToken cancellationToken = default)
{
    var response = _exchangeStore.Take(exchangeToken)
        ?? throw new UnauthorizedAccessException("Invalid exchange token.");
    return Task.FromResult(response);
}
```

- [ ] **Step 5: Extract token issuance helper from auth service**

```csharp
public interface IAuthService
{
    Task<AuthResponse> IssueAuthResponseAsync(User user, string? ipAddress);
}
```

```csharp
public Task<AuthResponse> IssueAuthResponseAsync(User user, string? ipAddress)
{
    return CreateAuthResponseAsync(user, ipAddress);
}
```

- [ ] **Step 6: Run backend build**

Run: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add backend/CourseVideo.API/Services/Google/GoogleUserInfo.cs backend/CourseVideo.API/Services/GoogleAuthService.cs backend/CourseVideo.API/Services/AuthService.cs backend/CourseVideo.API/Services/Interfaces/IAuthService.cs backend/CourseVideo.API.Tests/Services/GoogleAuthServiceTests.cs
git commit -m "feat: implement google oauth service"
```

### Task 4: Add Controller Endpoints and Frontend Redirect Contract

**Files:**
- Modify: `backend/CourseVideo.API/Controllers/AuthController.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/AuthControllerGoogleTests.cs`

- [ ] **Step 1: Write the failing callback redirect test**

```csharp
[Fact]
public async Task GoogleCallback_RedirectsToFrontendCallback_WithExchangeToken()
{
    var googleAuthService = new Mock<IGoogleAuthService>();
    googleAuthService.Setup(x => x.HandleCallbackAsync("code", "state", null, null, It.IsAny<CancellationToken>()))
        .ReturnsAsync("exchange-token");

    var controller = new AuthController(Mock.Of<IAuthService>(), googleAuthService.Object);

    var result = await controller.GoogleCallback("code", "state", null, null);

    result.Should().BeOfType<RedirectResult>();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter GoogleCallback_RedirectsToFrontendCallback_WithExchangeToken`
Expected: FAIL because Google controller endpoints do not exist yet

- [ ] **Step 3: Implement the endpoints**

```csharp
[HttpGet("google/login")]
public IActionResult GoogleLogin()
{
    var url = _googleAuthService.BuildAuthorizationUrl();
    return Redirect(url);
}

[HttpGet("google/callback")]
public async Task<IActionResult> GoogleCallback(
    [FromQuery] string? code,
    [FromQuery] string? state,
    [FromQuery] string? error,
    [FromQuery(Name = "error_description")] string? errorDescription,
    CancellationToken cancellationToken = default)
{
    try
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            throw new InvalidOperationException("google_auth_failed");
        }

        var exchangeToken = await _googleAuthService.HandleCallbackAsync(code, state, error, errorDescription, cancellationToken);
        return Redirect($"{_frontendCallbackUrl}?exchangeToken={Uri.EscapeDataString(exchangeToken)}");
    }
    catch (InvalidOperationException exception)
    {
        return Redirect($"{_frontendCallbackUrl}?error={Uri.EscapeDataString(exception.Message)}");
    }
}
```

- [ ] **Step 4: Run backend build**

Run: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Controllers/AuthController.cs backend/CourseVideo.API.Tests/Controllers/AuthControllerGoogleTests.cs
git commit -m "feat: add google auth controller endpoints"
```

### Task 5: Wire Frontend Auth Service and Callback Page

**Files:**
- Modify: `frontend/src/auth/authService.js`
- Modify: `frontend/src/auth/AuthContext.jsx`
- Create: `frontend/src/pages/GoogleAuthCallbackPage.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`
- Test: `frontend/src/pages/GoogleAuthCallbackPage.test.jsx`

- [ ] **Step 1: Write the failing callback-page test**

```jsx
it("exchanges the google exchange token and redirects admin to dashboard", async () => {
  mockExchangeGoogleLogin.mockResolvedValue({
    accessToken: "access",
    refreshToken: "refresh",
    user: { id: "1", fullName: "Admin", email: "admin@example.com", role: "Admin" }
  });

  render(
    <MemoryRouter initialEntries={["/auth/google/callback?exchangeToken=token-1"]}>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </MemoryRouter>
  );

  await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith("/dashboard", expect.anything()));
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- GoogleAuthCallbackPage.test.jsx --run`
Expected: FAIL because callback page and exchange helper do not exist

- [ ] **Step 3: Add exchange helper and auth-context session setter**

```js
export async function exchangeGoogleLogin(exchangeToken) {
  const { data } = await axiosClient.post("/auth/google/exchange", { exchangeToken });
  return data;
}
```

```jsx
function persistSession(response) {
  const nextSession = {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    user: response.user
  };

  saveAuthSession(nextSession);
  setSession(nextSession);
  return nextSession;
}

async function completeGoogleLogin(exchangeToken) {
  const response = await authService.exchangeGoogleLogin(exchangeToken);
  return persistSession(response);
}
```

- [ ] **Step 4: Create the callback page**

```jsx
export default function GoogleAuthCallbackPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { completeGoogleLogin } = useAuth();
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const exchangeToken = params.get("exchangeToken");
    const error = params.get("error");

    if (error) {
      setErrorMessage(mapGoogleError(error));
      return;
    }

    if (!exchangeToken) {
      setErrorMessage("Thiếu thông tin đăng nhập Google.");
      return;
    }

    void completeGoogleLogin(exchangeToken)
      .then((session) => {
        navigate(session.user?.role === "Admin" ? "/dashboard" : "/", { replace: true });
      })
      .catch(() => {
        setErrorMessage("Đăng nhập Google thất bại. Vui lòng thử lại.");
      });
  }, [completeGoogleLogin, location.search, navigate]);
}
```

- [ ] **Step 5: Register the route**

```jsx
<Route path="/auth/google/callback" element={<GoogleAuthCallbackPage />} />
```

- [ ] **Step 6: Run focused frontend test**

Run: `npm run test -- GoogleAuthCallbackPage.test.jsx --run`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add frontend/src/auth/authService.js frontend/src/auth/AuthContext.jsx frontend/src/pages/GoogleAuthCallbackPage.jsx frontend/src/routes/AppRoutes.jsx frontend/src/pages/GoogleAuthCallbackPage.test.jsx
git commit -m "feat: add frontend google auth callback flow"
```

### Task 6: Activate Google Button on Login and Surface Errors

**Files:**
- Modify: `frontend/src/components/auth/AuthShell.jsx`
- Modify: `frontend/src/pages/LoginPage.jsx`
- Modify: `frontend/src/pages/LoginPage.test.jsx`

- [ ] **Step 1: Write the failing login-page test**

```jsx
it("renders a real google login action", () => {
  render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>
  );

  const googleButton = screen.getByRole("link", { name: /google/i });
  expect(googleButton).toHaveAttribute("href", "http://localhost:5000/api/auth/google/login");
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- LoginPage.test.jsx --run`
Expected: FAIL because Google button is still an inert button

- [ ] **Step 3: Make the Google button configurable and activate it**

```jsx
function SocialButton({ href, icon, label, onClick }) {
  if (href) {
    return (
      <a className={styles.socialButton} href={href}>
        <span aria-hidden="true" className={styles.socialIcon}>{icon}</span>
        <span>{label}</span>
      </a>
    );
  }

  return (
    <button className={styles.socialButton} onClick={onClick} type="button">
      <span aria-hidden="true" className={styles.socialIcon}>{icon}</span>
      <span>{label}</span>
    </button>
  );
}
```

```jsx
<AuthShell googleAuthUrl={`${import.meta.env.VITE_API_BASE_URL?.replace(/\/api$/, "") || "http://localhost:5000"}/api/auth/google/login`} />
```

```jsx
const oauthErrorMessage = location.state?.oauthError ?? "";
const effectiveErrorMessage = errorMessage || oauthErrorMessage;
```

- [ ] **Step 4: Run focused frontend tests**

Run: `npm run test -- LoginPage.test.jsx GoogleAuthCallbackPage.test.jsx --run`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/auth/AuthShell.jsx frontend/src/pages/LoginPage.jsx frontend/src/pages/LoginPage.test.jsx
git commit -m "feat: activate google login entrypoint"
```

### Task 7: End-to-End Verification and Documentation

**Files:**
- Modify: `.env.example`
- Optional verify: local `.env`

- [ ] **Step 1: Confirm Google config placeholders are present**

```env
GoogleAuth__ClientId=your_google_client_id
GoogleAuth__ClientSecret=your_google_client_secret
GoogleAuth__FrontendCallbackUrl=http://localhost:3000/auth/google/callback
```

- [ ] **Step 2: Run backend build**

Run: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`
Expected: PASS

- [ ] **Step 3: Run frontend build**

Run: `npm run build`
Expected: PASS

- [ ] **Step 4: Run focused frontend tests**

Run: `npm run test -- LoginPage.test.jsx GoogleAuthCallbackPage.test.jsx --run`
Expected: PASS

- [ ] **Step 5: Smoke test manually**

Run:

```bash
docker compose up -d --build backend frontend
```

Verify:

- Visit `http://localhost:3000/login`
- Click `Google`
- Confirm browser redirects to Google consent screen
- After accepting, confirm redirect to `/auth/google/callback`
- Confirm admin/user lands on correct destination
- Confirm locked user is rejected with friendly message

- [ ] **Step 6: Commit**

```bash
git add .env.example
git commit -m "test: verify google login flow"
```

## Self-Review

- Spec coverage: the plan covers backend OAuth start/callback, one-time exchange token, local user create-or-login, frontend callback route, login-page activation, error mapping, and verification. No spec requirement is left without a task.
- Placeholder scan: removed generic TODO phrasing and included concrete files, commands, and code snippets for each task.
- Type consistency: the plan consistently uses `IGoogleAuthService`, `GoogleExchangeRequest`, `GoogleAuthOptions`, `GoogleAuthCallbackPage`, and `exchangeGoogleLogin`.
